using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using OpenTelemetry;

namespace GitHubStatsWebApi.Telemetry;

public class SignalRHubExporter(IHubContext<TelemetryHub, ITelemetryClient> hub) : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        using var scope = SuppressInstrumentationScope.Begin();

        foreach (var activity in batch)
        {
            var data = new TelemetryData(
                TraceId: activity.TraceId.ToString(),
                ActivityStartTime: activity.StartTimeUtc,
                ActivityId: activity.Id!,
                ActivityName: activity.DisplayName,
                TotalDuration: activity.Duration,
                ParentId: activity.ParentId,
                SourceName: activity.Source.Name,
                Status: activity.Status.ToString(),
                Extras: activity.StatusDescription is {} statusDescription 
                    ? new [] { statusDescription } 
                    : null);

            hub.Clients.All.ReceiveTelemetry(data);
        }

        return ExportResult.Success;
    }
}
