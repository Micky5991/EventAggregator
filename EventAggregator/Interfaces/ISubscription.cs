using System;
using Micky5991.EventAggregator.Elements;

namespace Micky5991.EventAggregator.Interfaces;

/// <summary>
/// Instance that represents a single subscription which can be unsubscribed from.
/// </summary>
public interface ISubscription : IDisposable
{
    /// <summary>
    /// Gets the configuration of this specific subscription.
    /// </summary>
    SubscriptionOptions SubscriptionOptions { get; }

    /// <summary>
    /// Gets a reference to the wanted type of this handler.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets a value indicating whether the current object has been disposed.
    /// </summary>
    bool IsDisposed { get; }
}
