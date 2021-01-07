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
    public class Subscription<T> : ISubscription
        where T : IEvent
    {
        private readonly ILogger<ISubscription> logger;

        private readonly IEventAggregator.EventHandlerDelegate<T> handler;

        private readonly ThreadTarget threadTarget;

        private readonly SynchronizationContext context;

        private readonly Action unsubscribeAction;

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription{T}"/> class.
        /// </summary>
        /// <param name="logger">Logger that should receive </param>
        /// <param name="handler">Callback that should be called upon publish.</param>
        /// <param name="threadTarget">Selected Thread where this subscription should be executed.</param>
        /// <param name="context">Context that will be needed for certain <paramref name="threadTarget"/> selections.</param>
        /// <param name="unsubscribeAction">Action that will be called when this subscription should not be called anymore.</param>
        /// <exception cref="ArgumentOutOfRangeException"><param name="threadTarget"></param> is invalid.</exception>
        public Subscription(
            ILogger<ISubscription> logger,
            IEventAggregator.EventHandlerDelegate<T> handler,
            ThreadTarget threadTarget,
            SynchronizationContext context,
            Action unsubscribeAction)
        {
            if (Enum.IsDefined(typeof(ThreadTarget), (int)threadTarget) == false)
            {
                throw new ArgumentOutOfRangeException(
                                                      nameof(threadTarget),
                                                      threadTarget,
                                                      $"{nameof(threadTarget)} is not defined in {typeof(ThreadTarget)}");
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.logger = logger;
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.threadTarget = threadTarget;
            this.context = context;
            this.unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
        }

        /// <summary>
        /// Calls the saved handler in a certain context.
        /// </summary>
        /// <param name="eventInstance">Event instance that should be passed to a handler that contains certain information.</param>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="threadTarget"/> is not handled.</exception>
        /// <exception cref="ObjectDisposedException"><see cref="Subscription{T}"/> has already been disposed.</exception>
        public void Invoke(T eventInstance)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(Subscription<T>));
            }

            if (eventInstance == null)
            {
                throw new ArgumentNullException(nameof(eventInstance));
            }

            switch (this.threadTarget)
            {
                case ThreadTarget.PublisherThread:
                    this.ExecuteSafely(eventInstance);

                    break;

                case ThreadTarget.MainThread:
                    this.context.Post(o => this.ExecuteSafely(eventInstance), null);

                    break;

                case ThreadTarget.BackgroundThread:
                    Task.Run(() => this.ExecuteSafely(eventInstance));

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(Subscription<T>));
            }

            this.unsubscribeAction();

            GC.SuppressFinalize(this);

            this.disposed = true;
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
    }
}
