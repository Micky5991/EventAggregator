using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Subscriptions;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Services
{
    internal class EventAggregatorService : IEventAggregator
    {
        private readonly ILogger<IEventAggregator> _logger;

        private readonly ConcurrentDictionary<Type, List<IInternalSubscription>> _subscriptions = new ConcurrentDictionary<Type, List<IInternalSubscription>>();

        public EventAggregatorService(ILogger<IEventAggregator> logger)
        {
            _logger = logger;
        }

        public ISubscription Subscribe<T>(EventAggregatorDelegates.AsyncEventCallback<T> asyncEventCallback,
            EventAggregatorDelegates.AsyncEventFilter<T> filter, EventPriority priority) where T : IEvent
        {
            if (asyncEventCallback == null)
            {
                throw new ArgumentNullException(nameof(asyncEventCallback));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            var subscription = new FilterableSubscription<T>(asyncEventCallback, filter, priority, this, _logger);

            return AddSubscription<T>(subscription);
        }

        private ISubscription AddSubscription<T>(IInternalSubscription subscription)
        {
            try
            {
                _subscriptions.AddOrUpdate(typeof(T), new List<IInternalSubscription>{ subscription }, (type, list) =>
                {
                    lock (list)
                    {
                        list.Add(subscription);
                        return list;
                    }
                });

                return subscription;
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occured during subscription of event \"{typeof(T)}\"", e);

                return null;
            }
        }

        public T Publish<T>(T eventData) where T : IEvent
        {
            Task.Run(() =>
            {
                try
                {
                    PublishByType(eventData.GetType(), eventData);
                    PublishByType(typeof(IEvent), eventData);
                }
                catch (Exception e)
                {
                    _logger.LogError($"An error occured during publish of event \"{typeof(T)}\"", e);
                }
            });

            return eventData;
        }

        private void PublishByType(Type eventType, IEvent eventData)
        {
            var subscriptions = GetOrderedSubscriptionSnapshot(eventType);
            if (subscriptions == null)
            {
                return;
            }

            foreach (var subscription in subscriptions)
            {
                try
                {
                    subscription.TriggerAsync(eventData);
                }
                catch (Exception e)
                {
                    _logger.LogError($"An error occured during publish of event \"{eventType}\"", e);
                }
            }
        }

        public async Task<T> PublishAsync<T>(T eventData) where T : IEvent
        {
            await Task.Run(async () =>
            {
                try
                {
                    await PublishByTypeAsync(eventData.GetType(), eventData);
                    await PublishByTypeAsync(typeof(IEvent), eventData);
                }
                catch (Exception e)
                {
                    _logger.LogError($"An error occured during publish of event \"{typeof(T)}\"", e);
                }
            });

            return eventData;
        }

        private async Task PublishByTypeAsync(Type eventType, IEvent eventData)
        {
            var subscriptions = GetOrderedSubscriptionSnapshot(eventType);
            if (subscriptions == null)
            {
                return;
            }

            foreach (var subscription in subscriptions)
            {
                try
                {
                    await subscription.TriggerAsync(eventData);
                }
                catch (Exception e)
                {
                    _logger.LogError($"An error occured during publish of event \"{eventType}\"", e);
                }
            }
        }

        private List<IInternalSubscription> GetOrderedSubscriptionSnapshot(Type eventType)
        {
            if (!_subscriptions.TryGetValue(eventType, out var typeSubscriptions))
            {
                return null;
            }

            lock (typeSubscriptions)
            {
                return new List<IInternalSubscription>(typeSubscriptions.OrderBy(s => s.Priority));
            }
        }

        public void Unsubscribe(IInternalSubscription internalSubscription)
        {
            if (!_subscriptions.TryGetValue(internalSubscription.EventType, out var typeSubscriptions))
            {
                return;
            }

            lock (typeSubscriptions)
            {
                typeSubscriptions.Remove(internalSubscription);

                if (typeSubscriptions.Any())
                {
                    return;
                }

                _subscriptions.TryRemove(internalSubscription.EventType, out _);
            }
        }
    }
}
