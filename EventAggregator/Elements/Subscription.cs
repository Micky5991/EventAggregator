using System;
using System.Threading;
using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Elements
{
    /// <summary>
    /// Class that represents a single subscription.
    /// </summary>
    /// <typeparam name="T">Event that will be represented by this subscription.</typeparam>
    public class Subscription<T> : IInternalSubscription
        where T : class, IEvent
    {
        private readonly ILogger<ISubscription> logger;

        private readonly IEventAggregator.EventHandlerDelegate<T> handler;

        private readonly SynchronizationContext? context;

        private readonly Action unsubscribeAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription{T}"/> class.
        /// </summary>
        /// <param name="logger">Logger that should receive.</param>
        /// <param name="handler">Callback that should be called upon publish.</param>
        /// <param name="ignoreCancelled">Ignore event subscription if event was cancelled before.</param>
        /// <param name="eventPriority">Priority of this subscription.</param>
        /// <param name="threadTarget">Selected Thread where this subscription should be executed.</param>
        /// <param name="context">Context that will be needed for <paramref name="threadTarget"/> selections.</param>
        /// <param name="unsubscribeAction">Action that will be called when this subscription should not be called anymore.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="threadTarget"></paramref> is invalid.</exception>
        public Subscription(
            ILogger<ISubscription> logger,
            IEventAggregator.EventHandlerDelegate<T> handler,
            bool ignoreCancelled,
            EventPriority eventPriority,
            ThreadTarget threadTarget,
            SynchronizationContext? context,
            Action unsubscribeAction)
        {
            if (Enum.IsDefined(typeof(ThreadTarget), (int)threadTarget) == false)
            {
                throw new ArgumentOutOfRangeException(
                                                      nameof(threadTarget),
                                                      threadTarget,
                                                      $"{nameof(threadTarget)} is not defined in {typeof(ThreadTarget)}");
            }

            if (Enum.IsDefined(typeof(EventPriority), (int)eventPriority) == false)
            {
                throw new ArgumentOutOfRangeException(
                                                      nameof(eventPriority),
                                                      eventPriority,
                                                      $"{nameof(eventPriority)} is not defined in {typeof(EventPriority)}");
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.logger = logger;
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.context = context;
            this.unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));

            this.IgnoreCancelled = ignoreCancelled;
            this.Priority = eventPriority;
            this.ThreadTarget = threadTarget;
            this.Type = typeof(T);

            this.ValidateSubscription();
        }

        /// <inheritdoc/>
        public bool IgnoreCancelled { get; }

        /// <inheritdoc/>
        public EventPriority Priority { get; }

        /// <inheritdoc/>
        public ThreadTarget ThreadTarget { get; }

        /// <inheritdoc/>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public Type Type { get; }

        /// <summary>
        /// Calls the saved handler in a certain context.
        /// </summary>
        /// <param name="eventInstance">Event instance that should be passed to a handler that contains certain information.</param>
        /// <exception cref="ObjectDisposedException"><see cref="Subscription{T}"/> has already been disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SynchronizationContext"/> is null, but <see cref="ThreadTarget"/> was set to main thread.</exception>
        public void Invoke(IEvent eventInstance)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Subscription<T>));
            }

            if (eventInstance == null)
            {
                throw new ArgumentNullException(nameof(eventInstance));
            }

            // Could not use '== false', because scope for instance is not right.
            if (!(eventInstance is T instance))
            {
                throw new ArgumentException("Type of event is invalid.", nameof(eventInstance));
            }

            switch (this.ThreadTarget)
            {
                case ThreadTarget.PublisherThread:
                    this.ExecuteSafely(instance);

                    break;

                case ThreadTarget.MainThread:
                    if (this.context == null)
                    {
                        throw new
                            InvalidOperationException($"Could not invoke subscription on {nameof(ThreadTarget.MainThread)} without {nameof(SynchronizationContext)} set.");
                    }

                    this.context.Post(_ => this.ExecuteSafely(instance), null);

                    break;

                case ThreadTarget.BackgroundThread:
                    Task.Run(() => this.ExecuteSafely(instance));

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Subscription<T>));
            }

            this.unsubscribeAction();

            GC.SuppressFinalize(this);

            this.IsDisposed = true;
        }

        private bool IsCancellableEvent()
        {
            return this.Type.GetInterface(nameof(ICancellableEvent)) != null;
        }

        private void ExecuteSafely(T eventInstance)
        {
            try
            {
                this.handler(eventInstance);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "An error occured during {0} subscription", typeof(T));
            }
        }

        private void ValidateSubscription()
        {
            if (this.IsCancellableEvent() && this.ThreadTarget != ThreadTarget.PublisherThread)
            {
                throw new
                    InvalidOperationException($"This event implements {typeof(ICancellableEvent)} and needs to run in the publishers thread to work.");
            }

            if (this.ThreadTarget == ThreadTarget.MainThread && this.context == null)
            {
                throw new
                    InvalidOperationException($"The {nameof(SynchronizationContext)} has to be set in order to use {nameof(ThreadTarget.MainThread)}");
            }
        }
    }
}
