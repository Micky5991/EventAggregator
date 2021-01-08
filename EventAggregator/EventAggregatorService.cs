using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly ILogger<IEventAggregator> logger;

        private readonly ILogger<ISubscription> subscriptionLogger;

        private SynchronizationContext? synchronizationContext;

        private IImmutableDictionary<Type, IImmutableList<IInternalSubscription>> handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventAggregatorService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance that should be used.</param>
        /// <param name="subscriptionLogger">Logger instance for the subscription that should be used.</param>
        public EventAggregatorService(ILogger<IEventAggregator> logger, ILogger<ISubscription> subscriptionLogger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.subscriptionLogger = subscriptionLogger ?? throw new ArgumentNullException(nameof(subscriptionLogger));

            this.handlers = new Dictionary<Type, IImmutableList<IInternalSubscription>>().ToImmutableDictionary();
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

            if (this.handlers.TryGetValue(typeof(T), out var publishHandler) == false)
            {
                return eventData;
            }

            foreach (var handler in publishHandler)
            {
                handler.Invoke(eventData);
            }

            return eventData;
        }

        /// <inheritdoc/>
        public ISubscription Subscribe<T>(
            IEventAggregator.EventHandlerDelegate<T> handler,
            EventPriority eventPriority,
            ThreadTarget threadTarget)
            where T : class, IEvent
        {
            var subscription = this.BuildSubscription(handler, eventPriority, threadTarget);

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

            return subscription;
        }

        /// <inheritdoc/>
        public void Subscribe(IEventListener eventListener)
        {
            throw new System.NotImplementedException();
        }

        private IInternalSubscription BuildSubscription<T>(
            IEventAggregator.EventHandlerDelegate<T> handler,
            EventPriority eventPriority,
            ThreadTarget threadTarget)
            where T : class, IEvent
        {
            return new Subscription<T>(
                                       this.subscriptionLogger,
                                       handler,
                                       eventPriority,
                                       threadTarget,
                                       this.synchronizationContext,
                                       () => { });
        }
    }
}
