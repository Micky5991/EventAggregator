using System;
using System.Threading.Tasks;

namespace Micky5991.EventAggregator.Interfaces
{
    internal interface IInternalSubscription : ISubscription
    {
        Type EventType { get; }

        EventPriority Priority { get; }

        Task TriggerAsync(object eventData);
    }
}
