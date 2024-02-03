using Micky5991.EventAggregator.Elements;

namespace Micky5991.EventAggregator.Sample.Events;

public class NotificationEvent : EventBase
{
    public NotificationEvent(int number)
    {
        Number = number;
    }

    public int Number { get; }
}
