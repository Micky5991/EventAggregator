using Micky5991.EventAggregator.Elements;

namespace EventAggregator.Tests.TestClasses
{
    public class TestEvent : EventBase
    {
        public int Number { get; }

        public TestEvent()
        {
            this.Number = 1234;
        }

        public TestEvent(int number) : this()
        {
            this.Number = number;
        }
    }
}
