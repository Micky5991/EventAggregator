using Micky5991.EventAggregator.Elements;

namespace Micky5991.EventAggregator.Sample.Events;

public class UserConnectedEvent : EventBase
{
    public UserConnectedEvent(string username)
    {
        Username = username;
    }

    public string Username { get; }
}