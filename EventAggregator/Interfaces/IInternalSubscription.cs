namespace Micky5991.EventAggregator.Interfaces
{
    /// <summary>
    /// Internal representation for testing purposes.
    /// </summary>
    internal interface IInternalSubscription : ISubscription
    {
        /// <summary>
        /// Triggers event handelrs with the given <paramref name="eventInstance"/>.
        /// </summary>
        /// <param name="eventInstance">Event data instance to send to handlers.</param>
        void Invoke(IEvent eventInstance);
    }
}
