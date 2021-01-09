using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Enums;
using Micky5991.EventAggregator.Interfaces;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator
{
    /// <inheritdoc cref="IEventAggregator"/>
    public class EventAggregatorService : IEventAggregator
    {
        private readonly ILogger<ISubscription> subscriptionLogger;

        private readonly ReaderWriterLock readerWriterLock;

        private SynchronizationContext? synchronizationContext;

        private IImmutableDictionary<Type, IImmutableList<IInternalSubscription>> handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventAggregatorService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance that should be used.</param>
        /// <param name="subscriptionLogger">Logger instance for the subscription that should be used.</param>
        public EventAggregatorService(ILogger<ISubscription> subscriptionLogger)
        {
            this.subscriptionLogger = subscriptionLogger ?? throw new ArgumentNullException(nameof(subscriptionLogger));

            this.handlers = new Dictionary<Type, IImmutableList<IInternalSubscription>>().ToImmutableDictionary();
            this.readerWriterLock = new ReaderWriterLock();
        }

        /// <inheritdoc />
        public void SetMainThreadSynchronizationContext(SynchronizationContext context)
        {
            this.synchronizationContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public T Publish<T>(T eventData)
            where T : class, IEvent
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            IImmutableList<IInternalSubscription> handlerList;

            this.readerWriterLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (this.handlers.TryGetValue(typeof(T), out handlerList) ==
                    false)
                {
                    return eventData;
                }
            }
            finally
            {
                this.readerWriterLock.ReleaseReaderLock();
            }

            foreach (var handler in handlerList)
            {
                if (handler.IsDisposed || (handler.IgnoreCancelled && eventData is ICancellableEvent { Cancelled: true }))
                {
                    continue;
                }

                handler.Invoke(eventData);
            }

            return eventData;
        }

        /// <inheritdoc/>
        public ISubscription Subscribe<T>(
            IEventAggregator.EventHandlerDelegate<T> handler,
            bool ignoreCancelled,
            EventPriority eventPriority,
            ThreadTarget threadTarget)
            where T : class, IEvent
        {
            var subscription = this.BuildSubscription(handler, ignoreCancelled, eventPriority, threadTarget);

            this.readerWriterLock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                if (this.handlers.TryGetValue(typeof(T), out var newHandlers) == false)
                {
                    newHandlers = new List<IInternalSubscription>
                    {
                        subscription,
                    }.ToImmutableList();
                }
                else
                {
                    newHandlers = newHandlers
                                  .Add(subscription)
                                  .OrderBy(x => x.Priority)
                                  .ToImmutableList();
                }

                this.handlers = this.handlers.SetItem(typeof(T), newHandlers);
            }
            finally
            {
                this.readerWriterLock.ReleaseWriterLock();
            }

            return subscription;
        }

        /// <inheritdoc/>
        public void Unsubscribe(ISubscription subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            subscription.Dispose();
        }

        private void InternalUnsubscribe(IInternalSubscription subscription)
        {
            if (subscription.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(subscription));
            }

            this.readerWriterLock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                if (this.handlers.TryGetValue(subscription.Type, out var handlerList) == false)
                {
                    return;
                }

                handlerList = handlerList.Remove(subscription);

                this.handlers = this.handlers.SetItem(subscription.Type, handlerList);
            }
            finally
            {
                this.readerWriterLock.ReleaseWriterLock();
            }
        }

        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "We WANT to modify the variable before use.")]
        private IInternalSubscription BuildSubscription<T>(
            IEventAggregator.EventHandlerDelegate<T> handler,
            bool ignoreCancelled,
            EventPriority eventPriority,
            ThreadTarget threadTarget)
            where T : class, IEvent
        {
            IInternalSubscription? subscription = null;

            subscription = new Subscription<T>(
                                               this.subscriptionLogger,
                                               handler,
                                               ignoreCancelled,
                                               eventPriority,
                                               threadTarget,
                                               this.synchronizationContext,
                                               () =>
                                               {
                                                   if (subscription == null)
                                                   {
                                                       throw new
                                                           InvalidOperationException($"Failed to remove subscription from {nameof(IEventAggregator)}");
                                                   }

                                                   this.InternalUnsubscribe(subscription);
                                               });

            return subscription;
        }
    }
}
