using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventAggregator.Tests.Exceptions;
using EventAggregator.Tests.TestClasses;
using FluentAssertions;
using Micky5991.EventAggregator;
using Micky5991.EventAggregator.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micky5991.EventAggregator.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventAggregator.Tests
{
    [TestClass]
    public class EventAggregatorFixture
    {
        private EventAggregatorService _eventAggregator;
        private Mock<ILogger<IEventAggregator>> _loggerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _loggerMock = new Mock<ILogger<IEventAggregator>>();

            _eventAggregator = new EventAggregatorService(_loggerMock.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _eventAggregator = null;

            _loggerMock = null;
        }

        [TestMethod]
        public void SubscribeToEventWillReturnSubscriptionObject()
        {
            var result = _eventAggregator.Subscribe<TestEvent>(TestCallback, null, EventPriority.Normal);

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<ISubscription>();
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void SubscribeToEventWillUpdateCache(int amount)
        {
            var subscriptions = new List<ISubscription>();

            for (var i = 0; i < amount; i++)
            {
                subscriptions.Add(_eventAggregator.Subscribe<TestEvent>(TestCallback, null, EventPriority.Normal));
            }

            var cache = _eventAggregator.OrderedSubscriptions;

            cache.Should().NotBeEmpty();
            cache.Should().HaveCount(1);
            cache.Should().ContainKey(typeof(TestEvent));

            var typeCache = cache[typeof(TestEvent)];

            typeCache.Should().Contain(subscriptions);
            typeCache.Count.Should().Be(amount);
        }

        [TestMethod]
        public void SubscriptionsShouldBeOrderedInCache()
        {
            _eventAggregator.Subscribe<TestEvent>(TestCallback, null, EventPriority.Normal);
            _eventAggregator.Subscribe<TestEvent>(TestCallback, null, EventPriority.High);
            _eventAggregator.Subscribe<TestEvent>(TestCallback, null, EventPriority.Low);

            var cache = _eventAggregator.OrderedSubscriptions[typeof(TestEvent)];
            cache.Should().BeInAscendingOrder(x => x.Priority);
        }

        [TestMethod]
        public void SubscribingWithNullCallbackThrowsArgumentNullException()
        {
            Action act = () => _eventAggregator.Subscribe<TestEvent>(null, null, EventPriority.Normal);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void UnsubscribeSubscriptionWillRemoveFromLists()
        {
            var subscription = (IInternalSubscription) _eventAggregator.Subscribe<TestEvent>(TestCallback, null, EventPriority.Normal);

            _eventAggregator.Unsubscribe(subscription);

            var subscriptions = _eventAggregator.Subscriptions;
            var cache = _eventAggregator.OrderedSubscriptions;

            subscriptions.Count.Should().Be(0);
            cache.Count.Should().Be(0);
        }

        [TestMethod]
        public void UnsubscribeWithUnknownSubscriptionWillDontChangeAnything()
        {
            var removedSubscription = (IInternalSubscription) _eventAggregator.Subscribe<TestEvent>(TestCallback, null, EventPriority.Normal);
            _eventAggregator.Subscribe<OtherTestEvent>(OtherTestCallback, null, EventPriority.Normal);

            var subscriptions = _eventAggregator.Subscriptions;
            var cache = _eventAggregator.OrderedSubscriptions;

            cache.Count.Should().Be(2);
            cache.Should().ContainKeys(typeof(TestEvent), typeof(OtherTestEvent));

            subscriptions.Count.Should().Be(2);
            subscriptions.Should().ContainKeys(typeof(TestEvent), typeof(OtherTestEvent));

            _eventAggregator.Unsubscribe(removedSubscription);

            cache.Count.Should().Be(1);
            cache.Should().ContainKeys(typeof(OtherTestEvent));

            subscriptions.Count.Should().Be(1);
            subscriptions.Should().ContainKeys(typeof(OtherTestEvent));

            _eventAggregator.Unsubscribe(removedSubscription);

            cache.Count.Should().Be(1);
            cache.Should().ContainKeys(typeof(OtherTestEvent));

            subscriptions.Count.Should().Be(1);
            subscriptions.Should().ContainKeys(typeof(OtherTestEvent));
        }

        [TestMethod]
        public async Task TriggerOnEventInterfaceWillTriggerAllEvents()
        {
            var callAmount = 0;

            Task Callback(IEvent eventData)
            {
                callAmount++;

                return Task.CompletedTask;
            }

            Task CallbackOther(TestEvent eventData)
            {
                callAmount++;

                return Task.CompletedTask;
            }

            _eventAggregator.Subscribe<IEvent>(Callback, null, EventPriority.Normal);
            _eventAggregator.Subscribe<TestEvent>(CallbackOther, null, EventPriority.Normal);

            await _eventAggregator.PublishAsync(new TestEvent());

            callAmount.Should().Be(2);
        }

        [TestMethod]
        public async Task ThrowingExceptionInHandlerDoesNotThrowException()
        {
            Task Callback(IEvent eventData)
            {
                throw new TestException();

                return Task.CompletedTask;
            }

            _eventAggregator.Subscribe<IEvent>(Callback, null, EventPriority.Normal);

            Func<Task> act = () => _eventAggregator.PublishAsync(new TestEvent());

            await act.Should().NotThrowAsync();
        }

        private Task TestCallback(TestEvent testEvent)
        {
            return Task.CompletedTask;
        }

        private Task OtherTestCallback(OtherTestEvent testEvent)
        {
            return Task.CompletedTask;
        }
    }
}
