using Micky5991.EventAggregator.Elements;

namespace Micky5991.EventAggregator.Sample.Events
{
    public class UserSendMessageEvent : CancellableEventBase
    {
        public UserSendMessageEvent(string username, string message)
        {
            this.Username = username;
            this.Message = message;
        }

        public string Username { get; }

        public string Message { get; }

    }
}
