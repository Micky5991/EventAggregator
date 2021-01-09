namespace Micky5991.EventAggregator.Interfaces
{
    /// <summary>
    /// Interface that signals, that this event allows expects a modified event instance, but this needs to happen in the
    /// publishers thread.
    /// </summary>
    public interface IDataChangingEvent : IEvent
    {
    }
}
