using System;
using System.Threading;
using System.Threading.Tasks;

namespace Micky5991.EventAggregator.Interfaces
{
    /// <summary>
    /// Type that handles loose event handling and created subscriptions.
    /// </summary>
    public interface IEventAggregator
    {
        /// <summary>
        /// Signature an event handler should have.
        /// </summary>
        /// <param name="eventData">Actual event data of this certain event.</param>
        /// <typeparam name="T">Type of event that was called.</typeparam>
        public delegate void EventHandlerDelegate<T>(T eventData)
            where T : class, IEvent;

        /// <summary>
        /// Signature an asynchronous event handler should have.
        /// </summary>
        /// <param name="eventData">Actual event data of this certain event.</param>
        /// <typeparam name="T">Type of event that was called.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public delegate Task AsyncEventHandlerDelegate<T>(T eventData)
            where T : class, IEvent;

        /// <summary>
        /// Sets the <see cref="SynchronizationContext"/> that should be used to trigger events in the main thread.
        /// </summary>
        /// <param name="synchronizationContext">Target context to execute the handler in.</param>
        /// <exception cref="ArgumentNullException"><paramref name="synchronizationContext"/> is null.</exception>
        void SetMainThreadSynchronizationContext(SynchronizationContext synchronizationContext);

        /// <summary>
        /// Publish event with this given eventdata.
        /// </summary>
        /// <param name="eventData">Event data that should be passed to all handlers.</param>
        /// <typeparam name="T">Type of the actual event data.</typeparam>
        /// <returns>Instance of event that was passed in <paramref name="eventData"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="eventData"/> is null.</exception>
        T Publish<T>(T eventData)
            where T : class, IEvent;

        /// <summary>
        /// Subscribes to the event of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="handler">Handler that should be executed on event publish.</param>
        /// <param name="ignoreCancelled">When the type <typeparamref name="T"/> implements <see cref="ICancellableEvent"/> and a lower priority cancels the event, this handler wont be invoked.</param>
        /// <param name="eventPriority">Defines a priority that orders handler execution from low to high.</param>
        /// <param name="threadTarget">Target thread that this event should be executed in.</param>
        /// <typeparam name="T">Type of event that will be executed.</typeparam>
        /// <exception cref="ArgumentException"><paramref name="threadTarget"/> is not PublisherThread.</exception>
        /// <exception cref="InvalidOperationException">Main thread synchronization has not been set, but <paramref name="threadTarget"/> was set to main thread.</exception>
        /// <returns>Subscription that has been created for this event handler.</returns>
        ISubscription Subscribe<T>(
            EventHandlerDelegate<T> handler,
            bool ignoreCancelled = false,
            EventPriority eventPriority = EventPriority.Normal,
            ThreadTarget threadTarget = ThreadTarget.PublisherThread)
            where T : class, IEvent;

        /// <summary>
        /// Subscribes to the event of type <typeparamref name="T"/>. This will not wait for any result and using
        /// <see cref="IDataChangingEvent"/> will result in unwanted behavior.
        /// </summary>
        /// <param name="handler">Handler that should be executed on event publish.</param>
        /// <param name="ignoreCancelled">When the type <typeparamref name="T"/> implements <see cref="ICancellableEvent"/> and a lower priority cancels the event, this handler wont be invoked.</param>
        /// <param name="eventPriority">Defines a priority that orders handler execution from low to high.</param>
        /// <param name="threadTarget">Target thread that this event should be executed in.</param>
        /// <typeparam name="T">Type of event that will be executed.</typeparam>
        /// <exception cref="ArgumentException"><paramref name="threadTarget"/> is not PublisherThread.</exception>
        /// <exception cref="InvalidOperationException">Main thread synchronization has not been set, but <paramref name="threadTarget"/> was set to main thread.</exception>
        /// <returns>Subscription that has been created for this event handler.</returns>
        ISubscription Subscribe<T>(
            AsyncEventHandlerDelegate<T> handler,
            bool ignoreCancelled = false,
            EventPriority eventPriority = EventPriority.Normal,
            ThreadTarget threadTarget = ThreadTarget.PublisherThread)
            where T : class, IEvent;

        /// <summary>
        /// Removes subscription from current aggregator.
        /// </summary>
        /// <param name="subscription">Subscription that should be unsubscribed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="subscription"/> is null.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="subscription"/> is already disposed.</exception>
        void Unsubscribe(ISubscription subscription);
    }
}
