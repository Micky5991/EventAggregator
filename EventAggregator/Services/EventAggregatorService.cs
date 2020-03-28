using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Subscriptions;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("EventAggregator.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Micky5991.EventAggregator.Services
{
    internal class EventAggregatorService : IEventAggregator
    {
        private readonly ILogger<IEventAggregator> _logger;

        private readonly ConcurrentDictionary<Type, List<IInternalSubscription>> _subscriptions = new ConcurrentDictionary<Type, List<IInternalSubscription>>();
        private readonly ConcurrentDictionary<Type, List<IInternalSubscription>> _orderedSubscriptions = new ConcurrentDictionary<Type, List<IInternalSubscription>>();

        private readonly ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim();

        internal ConcurrentDictionary<Type, List<IInternalSubscription>> Subscriptions => _subscriptions;
        internal ConcurrentDictionary<Type, List<IInternalSubscription>> OrderedSubscriptions => _orderedSubscriptions;


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

            var subscription = new AsyncSubscription<T>(asyncEventCallback, filter, priority, this, _logger);

            return AddSubscription<T>(subscription);
        }

        private ISubscription AddSubscription<T>(IInternalSubscription subscription)
        {
            var eventType = typeof(T);

            _readerWriterLock.EnterWriteLock();
            try
            {
                if (_subscriptions.TryGetValue(eventType, out var subscriptions) == false)
                {
                    _subscriptions.TryAdd(eventType, new List<IInternalSubscription> {subscription});

                    UpdateOrderedSubscriptionsCache(eventType);

                    return subscription;
                }

                subscriptions.Add(subscription);

                UpdateOrderedSubscriptionsCache(eventType);

                return subscription;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occured during subscription of event \"{typeof(T)}\"");

                return null;
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
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
                    _logger.LogError(e, $"An error occured during publish of event \"{typeof(T)}\"");
                }
            });

            return eventData;
        }

        private void PublishByType(Type eventType, IEvent eventData)
        {
            List<IInternalSubscription> subscriptions;

            _readerWriterLock.EnterReadLock();
            try
            {
                if (_orderedSubscriptions.TryGetValue(eventType, out subscriptions) == false)
                {
                    return;
                }
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }

            foreach (var subscription in subscriptions)
            {
                try
                {
                    subscription.TriggerAsync(eventData);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"An error occured during publish of event \"{eventType}\"");
                }
            }
        }

        public async Task<T> PublishAsync<T>(T eventData) where T : IEvent
        {
            try
            {
                await PublishByTypeAsync(eventData.GetType(), eventData);
                await PublishByTypeAsync(typeof(IEvent), eventData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occured during publish of event \"{typeof(T)}\"");
            }

            return eventData;
        }

        private async Task PublishByTypeAsync(Type eventType, IEvent eventData)
        {
            List<IInternalSubscription> subscriptions;

            _readerWriterLock.EnterReadLock();
            try
            {
                if (_orderedSubscriptions.TryGetValue(eventType, out subscriptions) == false)
                {
                    return;
                }
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }

            foreach (var subscription in subscriptions)
            {
                try
                {
                    await subscription.TriggerAsync(eventData);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"An error occured during publish of event \"{eventType}\"");
                }
            }
        }

        private void UpdateOrderedSubscriptionsCache(Type eventType)
        {
            if (_subscriptions.TryGetValue(eventType, out var typeSubscriptions) == false)
            {
                _orderedSubscriptions.TryRemove(eventType, out _);

                return;
            }

            _orderedSubscriptions[eventType] = new List<IInternalSubscription>(typeSubscriptions.OrderByDescending(s => s.Priority));
        }

        internal virtual void Unsubscribe(IInternalSubscription subscription)
        {
            _readerWriterLock.EnterWriteLock();
            try
            {
                var eventType = subscription.EventType;

                if (_subscriptions.TryGetValue(subscription.EventType, out var typeSubscriptions) == false)
                {
                    return;
                }

                typeSubscriptions.Remove(subscription);

                if (typeSubscriptions.Any() == false)
                {
                    _subscriptions.TryRemove(subscription.EventType, out _);
                }

                UpdateOrderedSubscriptionsCache(eventType);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }


        }
    }
}
