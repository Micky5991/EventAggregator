namespace Micky5991.EventAggregator.Interfaces
{
    /// <summary>
    /// Creates an event which can be cancelled.
    /// </summary>
    public interface ICancellableEvent : IEvent
    {
        public bool Cancelled { get; set; }
    }
}
