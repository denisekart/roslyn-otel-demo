using OpenTelemetry;

namespace GitHubStatsWebApi.Telemetry;

public class SignalRHubExporterProcessor(SignalRHubExporter exporter) : SimpleActivityExportProcessor(exporter)
{
}
