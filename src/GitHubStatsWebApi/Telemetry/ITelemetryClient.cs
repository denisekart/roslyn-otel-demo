namespace GitHubStatsWebApi.Telemetry;

public interface ITelemetryClient
{
    Task ReceiveTelemetry(TelemetryData telemetry);
}