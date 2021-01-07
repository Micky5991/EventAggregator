using System;

namespace Micky5991.EventAggregator.Interfaces
{
    /// <summary>
    /// Instance that represents a single subscription which can be unsubscribed from.
    /// </summary>
    public interface ISubscription : IDisposable
    {
    }
}
