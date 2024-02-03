namespace Micky5991.EventAggregator.Elements;

public class SubscriptionOptions
{

    /// <param name="ignoreCancelled">When the type <typeparamref name="T"/> implements <see cref="ICancellableEvent"/> and a lower priority cancels the event, this handler wont be invoked.</param>
    /// <param name="eventPriority">Defines a priority that orders handler execution from low to high.</param>
    /// <param name="threadTarget">Target thread that this event should be executed in.</param>

    public bool IgnoreCancelled { get; set; } = false;
    public EventPriority EventPriority { get; set; } = EventPriority.Normal;
    public ThreadTarget ThreadTarget { get; set; } = ThreadTarget.PublisherThread;
}
