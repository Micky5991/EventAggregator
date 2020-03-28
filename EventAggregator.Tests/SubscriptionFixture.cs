using System;
using System.Threading.Tasks;
using EventAggregator.Tests.Exceptions;
using EventAggregator.Tests.TestClasses;
using FluentAssertions;
using Micky5991.EventAggregator;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Services;
using Micky5991.EventAggregator.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace EventAggregator.Tests
{
    [TestClass]
    public class SubscriptionFixture
    {
        private int _calledAmount;

        private Mock<EventAggregatorService> _eventAggregator;

        private ILogger<IEventAggregator> _logger;

        [TestInitialize]
        public void Setup()
        {
            _logger = new NullLogger<IEventAggregator>();
            _eventAggregator = new Mock<EventAggregatorService>(_logger);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _calledAmount = 0;

            _eventAggregator = null;
            _logger = null;
        }

        [TestMethod]
        public async Task TriggeringAsyncWithNulledFilterWillIgnoreFilter()
        {
            var subscription = BuildAsyncSubscription<TestEvent>(IncreaseAmount, null);

            await subscription.TriggerAsync(new TestEvent());

            Assert.AreEqual(1, _calledAmount);
        }

        [TestMethod]
        public async Task TriggeringSyncWithNulledFilterWillIgnoreFilter()
        {
            var subscription = BuildSyncSubscription<TestEvent>(IncreaseAmountSync, null);

            await subscription.TriggerAsync(new TestEvent());
            subscription.TriggerSync(new TestEvent());

            Assert.AreEqual(2, _calledAmount);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public async Task TriggeringCertainAmountsOfEventsAsyncWillExecuteSameAmount(int amount)
        {
            var subscription = BuildAsyncSubscription<TestEvent>(IncreaseAmount, null);

            for (var i = 0; i < amount; i++)
            {
                await subscription.TriggerAsync(new TestEvent());
            }

            Assert.AreEqual(amount, _calledAmount);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public async Task TriggeringCertainAmountsOfEventsSyncWillExecuteSameAmount(int amount)
        {
            var subscription = BuildSyncSubscription<TestEvent>(IncreaseAmountSync, null);

            for (var i = 0; i < amount; i++)
            {
                await subscription.TriggerAsync(new TestEvent());
                subscription.TriggerSync(new TestEvent());
            }

            Assert.AreEqual(amount * 2, _calledAmount);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TriggeringWithFilterAsyncWillRespectGivenFilterResult(bool filterResult)
        {
            Task<bool> Filter(TestEvent eventData)
            {
                return Task.FromResult(filterResult);
            }

            var subscription = BuildAsyncSubscription<TestEvent>(IncreaseAmount, Filter);

            await subscription.TriggerAsync(new TestEvent());

            Assert.AreEqual(filterResult ? 1 : 0, _calledAmount);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TriggeringWithFilterSyncWillRespectGivenFilterResult(bool filterResult)
        {
            bool Filter(TestEvent eventData)
            {
                return filterResult;
            }

            var subscription = BuildSyncSubscription<TestEvent>(IncreaseAmountSync, Filter);

            await subscription.TriggerAsync(new TestEvent());
            subscription.TriggerSync(new TestEvent());

            Assert.AreEqual(filterResult ? 2 : 0, _calledAmount);
        }

        [TestMethod]
        public void DisposingAsyncSubscriptionWillUnregisterFromAggregator()
        {
            var subscription = BuildAsyncSubscription<TestEvent>(IncreaseAmount, null);

            _eventAggregator.Setup(x => x.Unsubscribe(subscription));

            subscription.Dispose();

            _eventAggregator.Verify(x => x.Unsubscribe(subscription), Times.Once);
        }

        [TestMethod]
        public void DisposingSyncSubscriptionWillUnregisterFromAggregator()
        {
            var subscription = BuildSyncSubscription<TestEvent>(IncreaseAmountSync, null);

            _eventAggregator.Setup(x => x.Unsubscribe(subscription));

            subscription.Dispose();

            _eventAggregator.Verify(x => x.Unsubscribe(subscription), Times.Once);
        }

        [TestMethod]
        public async Task ThrowingExceptionInFilterWillBeCatchedAndLogged()
        {
            Task<bool> Filter(TestEvent eventData)
            {
                throw new TestException();
            }

            var subscription = BuildAsyncSubscription<TestEvent>(IncreaseAmount, Filter);

            Func<Task> act = () => subscription.TriggerAsync(new TestEvent());

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task ThrowingExceptionInAsyncCallbackWillBeCatchedAndLogged()
        {
            Task Callback(TestEvent eventData)
            {
                return Task.FromException(new TestException());
            }

            var subscription = BuildAsyncSubscription<TestEvent>(Callback, null);

            Func<Task> act = () => subscription.TriggerAsync(new TestEvent());

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task ThrowingExceptionInSyncCallbackWillBeCatchedAndLogged()
        {
            void Callback(TestEvent eventData)
            {
                throw new TestException();
            }

            var subscription = BuildSyncSubscription<TestEvent>(Callback, null);

            Func<Task> act = () => subscription.TriggerAsync(new TestEvent());

            await act.Should().NotThrowAsync();
        }

        [DataTestMethod]
        [DataRow(EventPriority.Highest)]
        [DataRow(EventPriority.High)]
        [DataRow(EventPriority.Normal)]
        [DataRow(EventPriority.Low)]
        [DataRow(EventPriority.Lowest)]
        [DataRow(EventPriority.Monitor)]
        public void PriorityWillBeCorrectlySetupAsync(EventPriority priority)
        {
            var subscription = BuildAsyncSubscription<TestEvent>(IncreaseAmount, null, priority);

            subscription.Priority.Should().Be(priority);
        }

        [DataTestMethod]
        [DataRow(EventPriority.Highest)]
        [DataRow(EventPriority.High)]
        [DataRow(EventPriority.Normal)]
        [DataRow(EventPriority.Low)]
        [DataRow(EventPriority.Lowest)]
        [DataRow(EventPriority.Monitor)]
        public void PriorityWillBeCorrectlySetupSync(EventPriority priority)
        {
            var subscription = BuildSyncSubscription<TestEvent>(IncreaseAmountSync, null, priority);

            subscription.Priority.Should().Be(priority);
        }

        [TestMethod]
        public void EventTypeWillBeSetToGenericTypeParameterAsync()
        {
            var subscription = BuildAsyncSubscription<TestEvent>(IncreaseAmount, null);

            subscription.EventType.Should().Be(typeof(TestEvent));
        }

        [TestMethod]
        public void EventTypeWillBeSetToGenericTypeParameterSync()
        {
            var subscription = BuildSyncSubscription<TestEvent>(IncreaseAmountSync, null);

            subscription.EventType.Should().Be(typeof(TestEvent));
        }

        private AsyncSubscription<T> BuildAsyncSubscription<T>(EventAggregatorDelegates.AsyncEventCallback<T> callback,
            EventAggregatorDelegates.AsyncEventFilter<T> filter, EventPriority priority = EventPriority.Normal)
            where T : IEvent
        {
            return new AsyncSubscription<T>(callback, filter, priority, _eventAggregator.Object, _logger);
        }

        private SyncSubscription<T> BuildSyncSubscription<T>(EventAggregatorDelegates.EventCallback<T> callback,
            EventAggregatorDelegates.EventFilter<T> filter, EventPriority priority = EventPriority.Normal)
            where T : IEvent
        {
            return new SyncSubscription<T>(callback, filter, priority, _eventAggregator.Object, _logger);
        }

        private Task IncreaseAmount(TestEvent eventData)
        {
            _calledAmount++;

            return Task.CompletedTask;
        }

        private void IncreaseAmountSync(TestEvent eventData)
        {
            _calledAmount++;
        }

    }
}
