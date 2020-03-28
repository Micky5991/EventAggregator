using System;
using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Services;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Subscriptions
{
    internal class SyncSubscription<T> : AsyncSubscription<T>, IInternalSyncSubscription, ISyncSubscription where T : IEvent
    {

        private readonly EventAggregatorDelegates.EventCallback<T> _callback;
        private readonly EventAggregatorDelegates.EventFilter<T> _filter;

        internal SyncSubscription(
            EventAggregatorDelegates.EventCallback<T> callback,
            EventAggregatorDelegates.EventFilter<T> filter,
            EventPriority priority, EventAggregatorService eventAggregatorService,
            ILogger<IEventAggregator> logger
            )
            : base(null, null, priority, eventAggregatorService, logger)
        {
            _callback = callback;
            _filter = filter;
        }

        public override Task TriggerAsync(object eventData)
        {
            TriggerSync(eventData);

            return Task.CompletedTask;
        }

        public void TriggerSync(object eventData)
        {
            var convertedEventData = (T) eventData;

            if (_filter != null)
            {
                try
                {
                    if (_filter(convertedEventData) == false)
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
                _callback((T) eventData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occured during processing of event \"{typeof(T)}\".");
            };
        }
    }
}
