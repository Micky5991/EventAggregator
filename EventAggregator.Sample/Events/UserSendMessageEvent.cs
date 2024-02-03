using Micky5991.EventAggregator.Elements;

namespace Micky5991.EventAggregator.Sample.Events;

public class UserSendMessageEvent : CancellableEventBase
{
    public UserSendMessageEvent(string username, string message)
    {
        Username = username;
        Message = message;
    }

    public string Username { get; }

    public string Message { get; }

}
