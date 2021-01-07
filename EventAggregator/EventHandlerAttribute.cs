using System;
using Micky5991.EventAggregator.Enums;

namespace Micky5991.EventAggregator
{
    public class EventHandlerAttribute : Attribute
    {
        public EventPriority Priority { get; }

        public EventHandlerAttribute(EventPriority priority = EventPriority.Normal, ThreadTarget threadTarget = ThreadTarget.PublisherThread)
        {
            this.Priority = priority;
        }
    }
}
