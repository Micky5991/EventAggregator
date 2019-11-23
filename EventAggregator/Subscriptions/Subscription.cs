using System;
using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Services;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Subscriptions
{
    internal class Subscription<T> : ISubscription, IInternalSubscription where T : IEvent
    {
        private readonly EventAggregatorService _eventAggregatorService;
        protected readonly ILogger<IEventAggregator> _logger;

        private readonly EventAggregatorDelegates.AsyncEventCallback<T> _callback;

        public EventPriority Priority { get; }

        public Type EventType { get; } = typeof(T);

        internal Subscription(EventAggregatorDelegates.AsyncEventCallback<T> callback, EventPriority priority,
            EventAggregatorService eventAggregatorService, ILogger<IEventAggregator> logger)
        {
            _eventAggregatorService = eventAggregatorService;
            _logger = logger;

            _callback = callback;
            Priority = priority;
        }

        public virtual async Task TriggerAsync(object eventData)
        {
            try
            {
                await _callback((T) eventData);
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occured during processing of event \"{typeof(T)}\".", e);

                throw;
            }
        }

        public void Dispose()
        {
            _eventAggregatorService.Unsubscribe(this);
        }

    }
}
