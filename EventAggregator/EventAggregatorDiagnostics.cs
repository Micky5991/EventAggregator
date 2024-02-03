using System.Diagnostics;

namespace Micky5991.EventAggregator;

public static class EventAggregatorDiagnostics
{
    public const string SourceName = "EventAggregator";
    public static ActivitySource Source { get; } = new(SourceName);

    public const string TagEventType = "eventaggregator.eventtype";
    public const string TagOptionThreadTarget = "eventaggregator.option.threadtype";
    public const string TagOptionIgnoreCancelled = "eventaggregator.option.ignorecancelled";
    public const string TagOptionEventPriority = "eventaggregator.option.eventpriority";
    public const string TagHandlerMethod = "eventaggregator.threadtype";
}
