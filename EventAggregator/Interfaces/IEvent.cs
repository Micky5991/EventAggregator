namespace Micky5991.EventAggregator.Interfaces;

/// <summary>
/// Type that represents an event that is publishable with the <see cref="IEventAggregator"/>.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Indicates wether this certain event is cancellable.
    /// </summary>
    /// <returns>true if cancellable, false otherwise.</returns>
    bool IsCancellable();
}