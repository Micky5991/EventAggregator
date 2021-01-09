using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator.Elements
{
    public class EventBase : IEvent
    {
        /// <inheritdoc/>
        public bool IsCancellable()
        {
            return this.GetType().GetInterface(nameof(ICancellableEvent)) != null;
        }
    }
}
