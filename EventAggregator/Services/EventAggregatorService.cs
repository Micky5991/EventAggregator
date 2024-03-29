using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using CommunityToolkit.Diagnostics;
using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Interfaces;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Services;

/// <inheritdoc cref="IEventAggregator"/>
public class EventAggregatorService : IEventAggregator
{
    private readonly ILogger<ISubscription> _subscriptionLogger;

    private readonly ReaderWriterLock _readerWriterLock;

    private SynchronizationContext? _synchronizationContext;

    private IImmutableDictionary<Type, IImmutableList<IInternalSubscription>> _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventAggregatorService"/> class.
    /// </summary>
    /// <param name="subscriptionLogger">Logger instance for the subscription that should be used.</param>
    public EventAggregatorService(ILogger<ISubscription> subscriptionLogger)
    {
        _subscriptionLogger = subscriptionLogger ?? throw new ArgumentNullException(nameof(subscriptionLogger));

        _handlers = new Dictionary<Type, IImmutableList<IInternalSubscription>>().ToImmutableDictionary();
        _readerWriterLock = new ReaderWriterLock();
    }

    /// <inheritdoc />
    public void SetMainThreadSynchronizationContext(SynchronizationContext context)
    {
        Guard.IsNotNull(context);

        _synchronizationContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public T Publish<T>(T eventData)
        where T : class, IEvent
    {
        Guard.IsNotNull(eventData);

        using var activity = EventAggregatorDiagnostics.Source.StartActivity($"Publish event {typeof(T).FullName}");

        activity?.SetTag(EventAggregatorDiagnostics.TagEventType, typeof(T).FullName);

        EventAggregatorDiagnostics.PublishCount.Add(1, new KeyValuePair<string, object?>(EventAggregatorDiagnostics.TagEventType, typeof(T).FullName));

        IImmutableList<IInternalSubscription>? handlerList;

        _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
        try
        {
            if (_handlers.TryGetValue(eventData.GetType(), out handlerList) == false)
            {
                return eventData;
            }
        }
        finally
        {
            _readerWriterLock.ReleaseReaderLock();
        }

        var handleCount = 0;

        foreach (var handler in handlerList)
        {
            if (handler.IsDisposed || (handler.SubscriptionOptions.IgnoreCancelled && eventData is ICancellableEvent { Cancelled: true }))
            {
                continue;
            }

            handler.Invoke(eventData);

            handleCount++;
        }

        EventAggregatorDiagnostics.PublishHandlesCount.Add(handleCount, new KeyValuePair<string, object?>(EventAggregatorDiagnostics.TagEventType, typeof(T).FullName));

        return eventData;
    }

    /// <inheritdoc/>
    public ISubscription Subscribe<T>(IEventAggregator.EventHandlerDelegate<T> handler, SubscriptionOptions? subscriptionOptions = null)
        where T : class, IEvent
    {
        Guard.IsNotNull(handler);
        ValidateSubscriptionOptions(subscriptionOptions);

        var subscription = BuildSubscription(handler, subscriptionOptions);

        _readerWriterLock.AcquireWriterLock(Timeout.Infinite);

        try
        {
            if (_handlers.TryGetValue(typeof(T), out var newHandlers) == false)
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
                    .OrderBy(x => x.SubscriptionOptions.EventPriority)
                    .ToImmutableList();
            }

            _handlers = _handlers.SetItem(typeof(T), newHandlers);
        }
        finally
        {
            _readerWriterLock.ReleaseWriterLock();
        }

        return subscription;
    }

    public ISubscription Subscribe<T>(IEventAggregator.EventHandlerDelegate<T> handler, Action<SubscriptionOptions> configureSubscription) where T : class, IEvent
    {
        Guard.IsNotNull(handler);
        Guard.IsNotNull(configureSubscription);

        var options = new SubscriptionOptions();

        configureSubscription(options);

        ValidateSubscriptionOptions(options);

        return Subscribe(handler, options);
    }

    /// <inheritdoc />
    public ISubscription Subscribe<T>(IEventAggregator.AsyncEventHandlerDelegate<T> handler, SubscriptionOptions? subscriptionOptions = null)
        where T : class, IEvent
    {
        Guard.IsNotNull(handler);
        ValidateSubscriptionOptions(subscriptionOptions);

        async void ExecuteSubscription(T eventData)
        {
            try
            {
                await handler(eventData);
            }
            catch (Exception e)
            {
                _subscriptionLogger.LogError(e, "An error occured during async handler of {EventType}", typeof(T));
            }
        }

        return Subscribe<T>(ExecuteSubscription, subscriptionOptions);
    }

    public ISubscription Subscribe<T>(IEventAggregator.AsyncEventHandlerDelegate<T> handler, Action<SubscriptionOptions> configureSubscription) where T : class, IEvent
    {
        Guard.IsNotNull(handler);
        Guard.IsNotNull(configureSubscription);

        var options = new SubscriptionOptions();

        configureSubscription(options);

        ValidateSubscriptionOptions(options);

        return Subscribe(handler, options);
    }

    /// <inheritdoc/>
    public void Unsubscribe(ISubscription subscription)
    {
        Guard.IsNotNull(subscription);

        subscription.Dispose();
    }

    private void InternalUnsubscribe(IInternalSubscription subscription)
    {
        if (subscription.IsDisposed)
        {
            ThrowHelper.ThrowObjectDisposedException(nameof(subscription));
        }

        _readerWriterLock.AcquireWriterLock(Timeout.Infinite);

        try
        {
            if (_handlers.TryGetValue(subscription.Type, out var handlerList) == false)
            {
                return;
            }

            handlerList = handlerList.Remove(subscription);

            _handlers = _handlers.SetItem(subscription.Type, handlerList);
        }
        finally
        {
            _readerWriterLock.ReleaseWriterLock();
        }
    }

    private void ValidateSubscriptionOptions(SubscriptionOptions? options, [CallerArgumentExpression(nameof(options))] string? argument = null)
    {
        if (options == null)
        {
            return;
        }

        if (Enum.IsDefined(typeof(EventPriority), options.EventPriority) == false)
        {
            throw new ArgumentOutOfRangeException(argument, options.EventPriority, $"{nameof(options.EventPriority)} is not defined in {typeof(EventPriority)}");
        }

        if (Enum.IsDefined(typeof(ThreadTarget), options.ThreadTarget) == false)
        {
            throw new ArgumentOutOfRangeException(argument, options.ThreadTarget, $"{nameof(options.ThreadTarget)} is not defined in {typeof(ThreadTarget)}");
        }
    }

    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "We WANT to modify the variable before use.")]
    private IInternalSubscription BuildSubscription<T>(IEventAggregator.EventHandlerDelegate<T> handler, SubscriptionOptions? subscriptionOptions = null)
        where T : class, IEvent
    {
        Guard.IsNotNull(handler);

        IInternalSubscription? subscription = null;

        subscription = new Subscription<T>(
            _subscriptionLogger,
            handler,
            subscriptionOptions,
            _synchronizationContext,
            () =>
            {
                if (subscription == null)
                {
                    throw new
                        InvalidOperationException($"Failed to remove subscription from {nameof(IEventAggregator)}");
                }

                InternalUnsubscribe(subscription);
            });

        return subscription;
    }
}
