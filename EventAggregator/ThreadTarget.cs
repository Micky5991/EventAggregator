using System.Threading;
using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator
{
    /// <summary>
    /// Enum that defines where a certain subscription should be executed in.
    /// </summary>
    public enum ThreadTarget
    {
        /// <summary>
        /// This target takes the event and executes all handlers in the same thread where the publish method was called.
        /// You can send cancellable events in this thread.
        /// </summary>
        PublisherThread,

        /// <summary>
        /// Sends events in the main thread or any other important context like UI threads. The <see cref="IEventAggregator"/>
        /// needs a <see cref="SynchronizationContext"/> defined before first subscription.
        /// </summary>
        MainThread,

        /// <summary>
        /// Executes this subscription inside the ThreadPool in the background and the target thread does not matter.
        /// </summary>
        BackgroundThread,
    }
}
