
using System;
using System.Threading.Tasks;

namespace Micky5991.EventAggregator.Interfaces
{
    public interface IEventAggregator
    {
        /// <summary>
        /// Subscribes the given callback to the <see cref="IEventAggregator"/>, so it will betriggered when
        /// <see cref="Publish{T}"/> or <see cref="PublishAsync{T}"/> sends a new event.
        ///
        /// To listen to all events just subscribe to <see cref="IEvent"/>.
        /// </summary>
        /// <param name="asyncEventCallback">Callback to be triggered when event <typeparam name="T"></typeparam> was published</param>
        /// <param name="filter">Predicate to choose if the callback should be triggered or not</param>
        /// <param name="priority">Priority of this subscription, Highest will be called last</param>
        /// <typeparam name="T">EventType to subscribe to</typeparam>
        /// <exception cref="ArgumentNullException"><param name="asyncEventCallback"></param> is null</exception>
        /// <returns>Newly created subscription. Dispose to unsubscribe</returns>
        ISubscription Subscribe<T>(EventAggregatorDelegates.AsyncEventCallback<T> asyncEventCallback,
            EventAggregatorDelegates.AsyncEventFilter<T> filter = null, EventPriority priority = EventPriority.Normal) where T : IEvent;

        /// <summary>
        /// Publish event object without waiting for the result.
        /// </summary>
        /// <param name="eventData">Event data to send to all subscribers</param>
        /// <typeparam name="T">EventType that should be searched for and <see cref="IEvent"/>.</typeparam>
        /// <returns>Entered <param name="eventData"></param> to simplify usage</returns>
        T Publish<T>(T eventData) where T : IEvent;

        /// <summary>
        /// Publish event with waiting for every subscriber to complete and in respect of the given priority.
        /// </summary>
        /// <param name="eventData">Event data to send to all subscribers</param>
        /// <typeparam name="T">EventType that should be searched for and <see cref="IEvent"/>.</typeparam>
        /// <returns>Entered <param name="eventData"></param> to simplify usage</returns>
        Task<T> PublishAsync<T>(T eventData) where T : IEvent;

    }
}
