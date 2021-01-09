namespace Micky5991.EventAggregator.Interfaces
{
    /// <summary>
    /// Creates an event which can be cancelled. These events can only be executed from the publisherthread.
    /// </summary>
    public interface ICancellableEvent : IEvent
    {
        /// <summary>
        /// Gets or sets a value indicating whether this event has been cancelled or not.
        ///
        /// A <value>true</value> value usually means that this event does not execute all handlers.
        /// </summary>
        public bool Cancelled { get; set; }
    }
}
