
using System.Threading.Tasks;

namespace Micky5991.EventAggregator.Interfaces
{
    public interface IEventAggregator
    {
        ISubscription Subscribe<T>(EventAggregatorDelegates.AsyncEventCallback<T> asyncEventCallback,
            EventAggregatorDelegates.AsyncEventFilter<T> filter = null, EventPriority priority = EventPriority.Normal) where T : IEvent;

        T Publish<T>(T eventData) where T : IEvent;

        Task<T> PublishAsync<T>(T eventData) where T : IEvent;

    }
}
