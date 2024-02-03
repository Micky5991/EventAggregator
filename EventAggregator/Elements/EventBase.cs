using System.Linq;
using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator.Elements;

/// <summary>
/// Type that implements the <see cref="IEvent"/> interface. Should be used for all created events.
/// </summary>
public class EventBase : IEvent
{
    /// <inheritdoc/>
    public bool IsCancellable()
    {
        return GetType()
            .GetInterfaces().Contains(typeof(ICancellableEvent));
    }
}
