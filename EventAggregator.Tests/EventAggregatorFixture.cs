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
    }
}
