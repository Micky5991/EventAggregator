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
            var result = _eventAggregator.Subscribe<TestEvent>(TestCallbackAsync, null, EventPriority.Normal);

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<ISubscription>();
        }

        [TestMethod]
        public void SubscribeToSyncEventWillReturnSubscriptionObject()
        {
            var result = _eventAggregator.SubscribeSync<TestEvent>(TestCallbackSync, null, EventPriority.Normal);

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<ISyncSubscription>();
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
                subscriptions.Add(_eventAggregator.Subscribe<TestEvent>(TestCallbackAsync, null, EventPriority.Normal));
            }

            var cache = _eventAggregator.OrderedSubscriptions;

            cache.Should().NotBeEmpty();
            cache.Should().HaveCount(1);
            cache.Should().ContainKey(typeof(TestEvent));

            var typeCache = cache[typeof(TestEvent)];

            typeCache.Should().Contain(subscriptions);
            typeCache.Count.Should().Be(amount);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void SubscribeToSyncEventWillUpdateCache(int amount)
        {
            var subscriptions = new List<ISubscription>();

            for (var i = 0; i < amount; i++)
            {
                subscriptions.Add(_eventAggregator.SubscribeSync<TestEvent>(TestCallbackSync, null, EventPriority.Normal));
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
            _eventAggregator.Subscribe<TestEvent>(TestCallbackAsync, null, EventPriority.Normal);
            _eventAggregator.Subscribe<TestEvent>(TestCallbackAsync, null, EventPriority.High);
            _eventAggregator.Subscribe<TestEvent>(TestCallbackAsync, null, EventPriority.Low);

            var cache = _eventAggregator.OrderedSubscriptions[typeof(TestEvent)];
            cache.Should().BeInDescendingOrder(x => x.Priority);
        }

        [TestMethod]
        public void SyncSubscriptionsShouldBeOrderedInCache()
        {
            _eventAggregator.SubscribeSync<TestEvent>(TestCallbackSync, null, EventPriority.Normal);
            _eventAggregator.SubscribeSync<TestEvent>(TestCallbackSync, null, EventPriority.High);
            _eventAggregator.SubscribeSync<TestEvent>(TestCallbackSync, null, EventPriority.Low);

            var cache = _eventAggregator.OrderedSubscriptions[typeof(TestEvent)];
            cache.Should().BeInDescendingOrder(x => x.Priority);
        }

        [TestMethod]
        public void SubscribingWithNullCallbackThrowsArgumentNullException()
        {
            Action act = () => _eventAggregator.Subscribe<TestEvent>(null, null, EventPriority.Normal);
            Action actSync = () => _eventAggregator.SubscribeSync<TestEvent>(null, null, EventPriority.Normal);

            act.Should().Throw<ArgumentNullException>();
            actSync.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void UnsubscribeSubscriptionWillRemoveFromLists()
        {
            var subscription = (IInternalSubscription) _eventAggregator.Subscribe<TestEvent>(TestCallbackAsync, null, EventPriority.Normal);

            _eventAggregator.Unsubscribe(subscription);

            var subscriptions = _eventAggregator.Subscriptions;
            var cache = _eventAggregator.OrderedSubscriptions;

            subscriptions.Count.Should().Be(0);
            cache.Count.Should().Be(0);
        }

        [TestMethod]
        public void UnsubscribeSyncSubscriptionWillRemoveFromLists()
        {
            var subscription = (IInternalSubscription) _eventAggregator.SubscribeSync<TestEvent>(TestCallbackSync, null, EventPriority.Normal);

            _eventAggregator.Unsubscribe(subscription);

            var subscriptions = _eventAggregator.Subscriptions;
            var cache = _eventAggregator.OrderedSubscriptions;

            subscriptions.Count.Should().Be(0);
            cache.Count.Should().Be(0);
        }

        [TestMethod]
        public void UnsubscribeWithUnknownSubscriptionWillDontChangeAnything()
        {
            var removedSubscription = (IInternalSubscription) _eventAggregator.Subscribe<TestEvent>(TestCallbackAsync, null, EventPriority.Normal);
            _eventAggregator.Subscribe<OtherTestEvent>(OtherTestCallbackAsync, null, EventPriority.Normal);

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
        public void UnsubscribeWithUnknownSyncSubscriptionWillDontChangeAnything()
        {
            var removedSubscription = (IInternalSubscription) _eventAggregator.SubscribeSync<TestEvent>(TestCallbackSync, null, EventPriority.Normal);
            _eventAggregator.SubscribeSync<OtherTestEvent>(OtherTestCallbackSync, null, EventPriority.Normal);

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
            var callAmountAsync = 0;
            var callAmountSync = 0;

            Task CallbackAsync(IEvent eventData)
            {
                callAmountAsync++;

                return Task.CompletedTask;
            }

            Task CallbackOtherAsync(TestEvent eventData)
            {
                callAmountAsync++;

                return Task.CompletedTask;
            }

            void CallbackSync(IEvent eventData)
            {
                callAmountSync++;
            }

            void CallbackOtherSync(TestEvent eventData)
            {
                callAmountSync++;
            }

            _eventAggregator.Subscribe<IEvent>(CallbackAsync, null, EventPriority.Normal);
            _eventAggregator.Subscribe<TestEvent>(CallbackOtherAsync, null, EventPriority.Normal);
            _eventAggregator.SubscribeSync<IEvent>(CallbackSync, null, EventPriority.Normal);
            _eventAggregator.SubscribeSync<TestEvent>(CallbackOtherSync, null, EventPriority.Normal);

            await _eventAggregator.PublishAsync(new TestEvent());

            callAmountAsync.Should().Be(2);
            callAmountSync.Should().Be(2);
        }

        [TestMethod]
        public async Task ThrowingExceptionInHandlerDoesNotThrowException()
        {
            Task Callback(IEvent eventData)
            {
                throw new TestException();
            }

            _eventAggregator.Subscribe<IEvent>(Callback, null, EventPriority.Normal);

            Func<Task> act = () => _eventAggregator.PublishAsync(new TestEvent());

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public void ThrowingExceptionInSyncHandlerDoesNotThrowException()
        {
            void Callback(IEvent eventData)
            {
                throw new TestException();
            }

            _eventAggregator.SubscribeSync<IEvent>(Callback, null, EventPriority.Normal);

            Action act = () => _eventAggregator.PublishSync(new TestEvent());

            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task AsyncCallWillCallSyncAndAsyncSubscriptions()
        {
            var callAmountSync = 0;
            var callAmountAsync = 0;

            void Callback(IEvent eventData)
            {
                callAmountSync++;
            }

            Task CallbackAsync(IEvent eventData)
            {
                callAmountAsync++;

                return Task.CompletedTask;
            }

            _eventAggregator.SubscribeSync<TestEvent>(Callback, null, EventPriority.Normal);
            _eventAggregator.Subscribe<TestEvent>(CallbackAsync, null, EventPriority.Normal);

            await _eventAggregator.PublishAsync(new TestEvent());

            callAmountAsync.Should().Be(1);
            callAmountSync.Should().Be(1);
        }

        [TestMethod]
        public void SyncCallWillCallOnlySyncSubscriptions()
        {
            var callAmountSync = 0;
            var callAmountAsync = 0;

            void Callback(IEvent eventData)
            {
                callAmountSync++;
            }

            Task CallbackAsync(IEvent eventData)
            {
                callAmountAsync++;

                return Task.CompletedTask;
            }

            _eventAggregator.SubscribeSync<TestEvent>(Callback, null, EventPriority.Normal);
            _eventAggregator.Subscribe<TestEvent>(CallbackAsync, null, EventPriority.Normal);

            _eventAggregator.PublishSync(new TestEvent());

            callAmountAsync.Should().Be(0);
            callAmountSync.Should().Be(1);
        }

        private Task TestCallbackAsync(TestEvent testEvent)
        {
            return Task.CompletedTask;
        }

        private Task OtherTestCallbackAsync(OtherTestEvent testEvent)
        {
            return Task.CompletedTask;
        }

        private void TestCallbackSync(TestEvent testEvent)
        {
            // Do nothing
        }

        private void OtherTestCallbackSync(OtherTestEvent testEvent)
        {
            // Do Nothing
        }
    }
}
