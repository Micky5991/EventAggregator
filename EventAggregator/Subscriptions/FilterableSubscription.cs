using System;
using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Services;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Subscriptions
{
    internal class FilterableSubscription<T> : Subscription<T> where T : IEvent
    {
        private readonly EventAggregatorDelegates.AsyncEventFilter<T> _filter;

        public FilterableSubscription(EventAggregatorDelegates.AsyncEventCallback<T> callback,
            EventAggregatorDelegates.AsyncEventFilter<T> filter, EventPriority priority,
            EventAggregatorService eventAggregatorService, ILogger<IEventAggregator> logger)
            : base(callback, priority, eventAggregatorService, logger)
        {
            _filter = filter;
        }

        public override async Task TriggerAsync(object eventData)
        {
            var convertedEventData = (T) eventData;

            bool filterResult;
            try
            {
                filterResult = await _filter(convertedEventData);
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occured during filter check of event \"{typeof(T)}\"", e);

                throw;
            }

            if (filterResult == false)
            {
                return;
            }

            await base.TriggerAsync(convertedEventData);
        }
    }
}
