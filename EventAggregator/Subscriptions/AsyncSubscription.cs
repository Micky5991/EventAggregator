using System;
using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Services;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Subscriptions
{
    internal class AsyncSubscription<T> : ISubscription, IInternalSubscription where T : IEvent
    {
        private readonly EventAggregatorService _eventAggregatorService;
        protected readonly ILogger<IEventAggregator> _logger;

        private readonly EventAggregatorDelegates.AsyncEventCallback<T> _callback;
        private readonly EventAggregatorDelegates.AsyncEventFilter<T> _filter;

        public EventPriority Priority { get; }

        public Type EventType { get; } = typeof(T);

        internal AsyncSubscription(EventAggregatorDelegates.AsyncEventCallback<T> callback,
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
                try
                {
                    if (await _filter(convertedEventData) == false)
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"An error occured during filter check of event \"{typeof(T)}\"");
                }
            }

            try
            {
                await _callback((T) eventData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occured during processing of event \"{typeof(T)}\".");
            }
        }

        public void Dispose()
        {
            _eventAggregatorService.Unsubscribe(this);
        }

    }
}
