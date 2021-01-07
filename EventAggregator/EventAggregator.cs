using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator
{
    public class EventAggregator : IEventAggregator
    {
        public T Publish<T>(T eventData)
            where T : IEvent
        {
            throw new System.NotImplementedException();
        }

        public void Subscribe<T>(IEventAggregator.EventHandlerDelegate<T> handler, ThreadTarget threadTarget = ThreadTarget.PublisherThread)
            where T : IEvent
        {
            throw new System.NotImplementedException();
        }

        public void Subscribe(IEventListener eventListener)
        {
            throw new System.NotImplementedException();
        }
    }
}
