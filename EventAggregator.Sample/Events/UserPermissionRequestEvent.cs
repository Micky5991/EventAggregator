using Micky5991.EventAggregator.Elements;

namespace Micky5991.EventAggregator.Sample.Events;

public class UserPermissionRequestEvent : CancellableEventBase
{
    public UserPermissionRequestEvent(string username, string role)
    {
        Username = username;
        Role = role;
    }

    public string Username { get; }

    public string Role { get; }
}