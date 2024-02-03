using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator.Elements;

public class SubscriptionOptions
{
    /// <summary>
    /// Gets a value indicating when the eventtype implements <see cref="ICancellableEvent"/> and a lower priority handler cancels the event, this handler won't be invoked.
    /// </summary>
    public bool IgnoreCancelled { get; set; } = false;

    /// <summary>
    /// Gets the priority that orders handler execution from low to high.
    /// </summary>
    public EventPriority EventPriority { get; set; } = EventPriority.Normal;

    /// <summary>
    /// Gets the target thread where this event should be executed in.
    /// </summary>
    public ThreadTarget ThreadTarget { get; set; } = ThreadTarget.PublisherThread;
}
