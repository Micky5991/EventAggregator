using System;
using Micky5991.EventAggregator.Enums;

namespace Micky5991.EventAggregator.Interfaces
{
    /// <summary>
    /// Instance that represents a single subscription which can be unsubscribed from.
    /// </summary>
    public interface ISubscription : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the event should not be executed when the event was cancelled before.
        /// </summary>
        bool IgnoreCancelled { get; }

        /// <summary>
        /// Gets priority that this subscription should be called.
        /// </summary>
        EventPriority Priority { get; }

        /// <summary>
        /// Gets target where this eventhandler should be executed in.
        /// </summary>
        ThreadTarget ThreadTarget { get; }

        /// <summary>
        /// Gets a value indicating whether the current object has been disposed.
        /// </summary>
        bool IsDisposed { get; }
    }
}
