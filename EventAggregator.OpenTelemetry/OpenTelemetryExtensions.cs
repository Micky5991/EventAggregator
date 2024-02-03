using Micky5991.EventAggregator;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EventAggregator.OpenTelemetry;

public static class OpenTelemetryExtensions
{
    public static TracerProviderBuilder AddEventAggregatorInstrumentation(
        this TracerProviderBuilder tracerProviderBuilder)
    {
        return tracerProviderBuilder.AddSource(EventAggregatorDiagnostics.SourceName);
    }

    public static MeterProviderBuilder AddEventAggregatorInstrumentation(
        this MeterProviderBuilder meterProviderBuilder)
    {
        return meterProviderBuilder.AddMeter(EventAggregatorDiagnostics.SourceName);
    }
}
