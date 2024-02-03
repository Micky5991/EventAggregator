using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Micky5991.EventAggregator.Interfaces;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Elements;

/// <summary>
/// Class that represents a single subscription.
/// </summary>
/// <typeparam name="T">Event that will be represented by this subscription.</typeparam>
public class Subscription<T> : IInternalSubscription
    where T : class, IEvent
{
    private readonly ILogger<ISubscription> _logger;

    private readonly IEventAggregator.EventHandlerDelegate<T> _handler;

    private readonly SynchronizationContext? _context;

    private readonly Action _unsubscribeAction;

    /// <inheritdoc/>
    public SubscriptionOptions SubscriptionOptions { get; }

    /// <inheritdoc/>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public Type Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Subscription{T}"/> class.
    /// </summary>
    /// <param name="logger">Logger that should receive.</param>
    /// <param name="handler">Callback that should be called upon publish.</param>
    /// <param name="subscriptionOptions">Options to configure the behavior of this specific subscription.</param>
    /// <param name="context">Context that will be needed for specific thread selections in <paramref name="subscriptionOptions"/>.</param>
    /// <param name="unsubscribeAction">Action that will be called when this subscription should not be called anymore.</param>
    public Subscription(
        ILogger<ISubscription> logger,
        IEventAggregator.EventHandlerDelegate<T> handler,
        SubscriptionOptions? subscriptionOptions,
        SynchronizationContext? context,
        Action unsubscribeAction)
    {
        Guard.IsNotNull(logger);
        Guard.IsNotNull(handler);
        Guard.IsNotNull(unsubscribeAction);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _context = context;
        _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));

        SubscriptionOptions = subscriptionOptions ?? new SubscriptionOptions();

        Type = typeof(T);

        ValidateSubscription();
    }

    /// <summary>
    /// Calls the saved handler in a certain context.
    /// </summary>
    /// <param name="eventInstance">Event instance that should be passed to a handler that contains certain information.</param>
    /// <exception cref="ObjectDisposedException"><see cref="Subscription{T}"/> has already been disposed.</exception>
    /// <exception cref="InvalidOperationException"><see cref="SynchronizationContext"/> is null, but <see cref="ThreadTarget"/> was set to main thread.</exception>
    public void Invoke(IEvent eventInstance)
    {
        using var activity = EventAggregatorDiagnostics.Source.StartActivity($"Invoke handler {_handler.Method}");

        activity?.SetTag(EventAggregatorDiagnostics.TagEventType, typeof(T).FullName);
        activity?.SetTag(EventAggregatorDiagnostics.TagHandlerMethod, _handler.Method);
        activity?.SetTag(EventAggregatorDiagnostics.TagOptionEventPriority, SubscriptionOptions.EventPriority);
        activity?.SetTag(EventAggregatorDiagnostics.TagOptionIgnoreCancelled, SubscriptionOptions.IgnoreCancelled);
        activity?.SetTag(EventAggregatorDiagnostics.TagOptionThreadTarget, SubscriptionOptions.ThreadTarget);

        if (IsDisposed)
        {
            ThrowHelper.ThrowObjectDisposedException(nameof(Subscription<T>));
        }

        Guard.IsNotNull(eventInstance);

        if (eventInstance is not T instance)
        {
            throw new ArgumentException("Type of event is invalid.", nameof(eventInstance));
        }

        switch (SubscriptionOptions.ThreadTarget)
        {
            case ThreadTarget.PublisherThread:
                ExecuteSafely(instance);

                break;

            case ThreadTarget.MainThread:
                if (_context == null)
                {
                    throw new
                        InvalidOperationException($"Could not invoke subscription on {nameof(ThreadTarget.MainThread)} without {nameof(SynchronizationContext)} set.");
                }

                _context.Post(_ => ExecuteSafely(instance), null);

                break;

            case ThreadTarget.BackgroundThread:
                Task.Run(() => ExecuteSafely(instance));

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (IsDisposed)
        {
            ThrowHelper.ThrowObjectDisposedException(nameof(Subscription<T>));
        }

        _unsubscribeAction();

        GC.SuppressFinalize(this);

        IsDisposed = true;
    }

    private bool IsDataChaningEvent()
    {
        return Type.GetInterfaces().Contains(typeof(IDataChangingEvent));
    }

    private void ExecuteSafely(T eventInstance)
    {
        try
        {
            _handler(eventInstance);
        }
        catch (Exception e)
        {
            _logger.LogError(EventAggregatorErrors.ErrorDuringSubscriptionExecution, e,
                "An error occured during {EventType} subscription", typeof(T));
        }
    }

    private void ValidateSubscription()
    {
        if (IsDataChaningEvent() && SubscriptionOptions.ThreadTarget != ThreadTarget.PublisherThread)
        {
            throw new InvalidOperationException($"This event implements {typeof(IDataChangingEvent)} and needs to run in the publishers thread to work.");
        }

        if (SubscriptionOptions.ThreadTarget == ThreadTarget.MainThread && _context == null)
        {
            throw new InvalidOperationException($"The {nameof(SynchronizationContext)} has to be set in order to use {nameof(ThreadTarget.MainThread)}");
        }
    }
}
