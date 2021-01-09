using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator.Elements
{
    /// <summary>
    /// Base of an event that enables cancellation over the <see cref="ICancellableEvent"/> interface.
    /// </summary>
    public abstract class CancellableEventBase : EventBase, ICancellableEvent
    {
        /// <inheritdoc/>
        public bool Cancelled { get; set; }
    }
}
