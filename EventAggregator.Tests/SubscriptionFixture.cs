using System.Threading.Tasks;
using EventAggregator.Tests.TestClasses;
using Micky5991.EventAggregator;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Services;
using Micky5991.EventAggregator.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace EventAggregator.Tests
{
    [TestClass]
    public class SubscriptionFixture
    {
        private int _calledAmount;

        private Mock<ILogger<IEventAggregator>> _logger;
        private Mock<EventAggregatorService> _eventAggregator;

        [TestInitialize]
        public void Setup()
        {
            _logger = new Mock<ILogger<IEventAggregator>>();
            _eventAggregator = new Mock<EventAggregatorService>(_logger.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _calledAmount = 0;

            _eventAggregator = null;
            _logger = null;
        }

        [TestMethod]
        public async Task TriggeringWithNulledFilterWillIgnoreFilter()
        {
            var subscription = BuildSubscription<TestEvent>(IncreaseAmount, null);

            await subscription.TriggerAsync(new TestEvent());

            Assert.AreEqual(1, _calledAmount);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public async Task TriggeringCertainAmountsOfEventsWillExecuteSameAmount(int amount)
        {
            var subscription = BuildSubscription<TestEvent>(IncreaseAmount, null);

            for (var i = 0; i < amount; i++)
            {
                await subscription.TriggerAsync(new TestEvent());
            }

            Assert.AreEqual(amount, _calledAmount);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TriggeringWithFilterWillRespectGivenFilterResult(bool filterResult)
        {
            Task<bool> Filter(TestEvent eventData)
            {
                return Task.FromResult(filterResult);
            }

            var subscription = BuildSubscription<TestEvent>(IncreaseAmount, Filter);

            await subscription.TriggerAsync(new TestEvent());

            Assert.AreEqual(filterResult ? 1 : 0, _calledAmount);
        }

        [TestMethod]
        public void DisposingSubscriptionWillUnregisterFromAggregator()
        {
            var subscription = BuildSubscription<TestEvent>(IncreaseAmount, null);

            _eventAggregator.Setup(x => x.Unsubscribe(subscription));

            subscription.Dispose();

            _eventAggregator.Verify(x => x.Unsubscribe(subscription), Times.Once);
        }

        private Subscription<T> BuildSubscription<T>(EventAggregatorDelegates.AsyncEventCallback<T> callback, EventAggregatorDelegates.AsyncEventFilter<T> filter)
            where T : IEvent
        {
            return new Subscription<T>(callback, filter, EventPriority.Normal, _eventAggregator.Object, _logger.Object);
        }

        private async Task IncreaseAmount(TestEvent eventData)
        {
            _calledAmount++;
        }

    }
}
