namespace Micky5991.EventAggregator
{
    public static class EventAggregatorErrors
    {
        public const int ErrorDuringEventPublish = 2000;
        public const int ErrorDuringEventSubscription = 2001;

        public const int ErrorDuringSubscriptionExecution = 3000;
        public const int ErrorDuringSubscriptionFilter = 3001;
    }
}
