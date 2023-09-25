using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GitHubStatsWebApi.Telemetry;

internal static class TelemetryExtensions
{
    internal static void AddTelemetry(this WebApplicationBuilder? app)
    {
        app!.Services.AddSingleton<SignalRHubExporter>();
        
        // TODO: Add telemetry here
    }
}
