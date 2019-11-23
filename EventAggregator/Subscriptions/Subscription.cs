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
        private readonly ILogger<IEventAggregator> _logger;

        private readonly EventAggregatorDelegates.AsyncEventCallback<T> _callback;
        private readonly EventAggregatorDelegates.AsyncEventFilter<T> _filter;

        public EventPriority Priority { get; }

        public Type EventType { get; } = typeof(T);

        internal Subscription(EventAggregatorDelegates.AsyncEventCallback<T> callback,
            EventAggregatorDelegates.AsyncEventFilter<T> filter, EventPriority priority,
            EventAggregatorService eventAggregatorService, ILogger<IEventAggregator> logger)
        {
            _eventAggregatorService = eventAggregatorService;
            _logger = logger;

            _callback = callback;
            _filter = filter;
            Priority = priority;
        }

        public virtual async Task TriggerAsync(object eventData)
        {
            var convertedEventData = (T) eventData;

            if (_filter != null)
            {
                bool filterResult;
                try
                {
                    filterResult = await _filter(convertedEventData);
                }
                catch (Exception e)
                {
                    _logger.LogError($"An error occured during filter check of event \"{typeof(T)}\"", e);
                }

                if (filterResult == false)
                {
                    return;
                }
            }

            try
            {
                await _callback((T) eventData);
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occured during processing of event \"{typeof(T)}\".", e);
            }
        }

        public void Dispose()
        {
            _eventAggregatorService.Unsubscribe(this);
        }

    }
}
