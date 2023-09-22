namespace GitHubStatsWebApi.Telemetry;

public record TelemetryData(
    string TraceId,
    DateTime ActivityStartTime,
    string ActivityId,
    string ActivityName,
    TimeSpan TotalDuration,
    string? ParentId,
    string? SourceName,
    string? Status,
    string[]? Extras);
