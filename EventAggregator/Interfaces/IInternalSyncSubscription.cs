namespace Micky5991.EventAggregator.Interfaces
{
    internal interface IInternalSyncSubscription : IInternalSubscription
    {
        void TriggerSync(object eventData);
    }
}
