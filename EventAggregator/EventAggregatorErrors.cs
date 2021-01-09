using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator
{
    /// <summary>
    /// Contains all error messages the <see cref="IEventAggregator"/> will ever send.
    /// </summary>
    public static class EventAggregatorErrors
    {
        /// <summary>
        /// Will be sent if a <see cref="ISubscription"/> catches an exception inside handler.
        /// </summary>
        public const int ErrorDuringSubscriptionExecution = 3000;
    }
}
