using System;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator.Elements;

public class SubscriptionOptions
{
    private EventPriority _eventPriority = EventPriority.Normal;
    private ThreadTarget _threadTarget = ThreadTarget.PublisherThread;

    /// <summary>
    /// Gets a value indicating when the eventtype implements <see cref="ICancellableEvent"/> and a lower priority handler cancels the event, this handler won't be invoked.
    /// </summary>
    public bool IgnoreCancelled { get; set; } = false;

    /// <summary>
    /// Gets the priority that orders handler execution from low to high.
    /// </summary>
    public EventPriority EventPriority
    {
        get => _eventPriority;
        set
        {
            ValidateEventPriority(value);

            _eventPriority = value;
        }
    }

    /// <summary>
    /// Gets the target thread where this event should be executed in.
    /// </summary>
    public ThreadTarget ThreadTarget
    {
        get => _threadTarget;
        set
        {
            ValidateThreadTarget(value);

            _threadTarget = value;
        }
    }

    private static void ValidateEventPriority(EventPriority priority, [CallerArgumentExpression(nameof(priority))] string? argument = null)
    {
        if (Enum.IsDefined(typeof(EventPriority), priority) == false)
        {
            throw new ArgumentOutOfRangeException(argument, priority, $"{priority} is not defined in {typeof(EventPriority)}");
        }
    }

    private static void ValidateThreadTarget(ThreadTarget threadTarget, [CallerArgumentExpression(nameof(threadTarget))] string? argument = null)
    {
        if (Enum.IsDefined(typeof(ThreadTarget), threadTarget) == false)
        {
            throw new ArgumentOutOfRangeException(argument, threadTarget, $"{threadTarget} is not defined in {typeof(EventPriority)}");
        }
    }
}
