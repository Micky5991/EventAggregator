namespace Micky5991.EventAggregator.Interfaces
{
    public interface IEventAggregator
    {
        public delegate void EventHandlerDelegate<T>(T eventData)
            where T : IEvent;

        public T Publish<T>(T eventData)
            where T : IEvent;

        public void Subscribe<T>(EventHandlerDelegate<T> handler, ThreadTarget threadTarget = ThreadTarget.PublisherThread)
            where T : IEvent;

        public void Subscribe(IEventListener eventListener);
    }
}
