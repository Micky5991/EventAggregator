using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Micky5991.EventAggregator;

public static class EventAggregatorDiagnostics
{
    public const string SourceName = "EventAggregator";
    public static ActivitySource Source { get; } = new(SourceName);
    public static Meter Meter { get; } = new(SourceName);

    public static Counter<long> PublishCount { get; } = Meter.CreateCounter<long>("eventaggregator.meter.publishcount");
    public static Counter<long> PublishHandlesCount { get; } = Meter.CreateCounter<long>("eventaggregator.meter.publishhandlescount");

    public const string TagEventType = "eventaggregator.eventtype";
    public const string TagOptionThreadTarget = "eventaggregator.option.threadtype";
    public const string TagOptionIgnoreCancelled = "eventaggregator.option.ignorecancelled";
    public const string TagOptionEventPriority = "eventaggregator.option.eventpriority";
    public const string TagHandlerMethod = "eventaggregator.threadtype";
}
