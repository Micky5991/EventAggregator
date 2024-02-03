using Micky5991.EventAggregator.Elements;

namespace Micky5991.EventAggregator.Tests.TestClasses;

public class TestEvent : EventBase
{
    public int Number { get; }

    public TestEvent()
    {
        Number = 1234;
    }

    public TestEvent(int number) : this()
    {
        Number = number;
    }
}