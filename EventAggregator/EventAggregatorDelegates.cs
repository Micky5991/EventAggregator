using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator
{
    public static class EventAggregatorDelegates
    {
        public delegate Task AsyncEventCallback<T>(T eventData) where T : IEvent;
        public delegate Task<bool> AsyncEventFilter<T>(T eventData) where T : IEvent;
    }
}
