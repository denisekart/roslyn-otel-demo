using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GitHubStatsWebApi.Telemetry;

internal static class TelemetryExtensions
{
    internal static void AddTelemetry(this WebApplicationBuilder? app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.Services.AddSingleton<SignalRHubExporter>();
        
        app.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddTelemetrySdk();
                resource.AddService(DefaultActivitySource.ActivitySource.Name, DefaultActivitySource.ActivitySource.Version);
            })
            .WithTracing(tracer =>
            {
                tracer.SetSampler(new AlwaysOnSampler());
                tracer.AddSource(DefaultActivitySource.ActivitySource.Name);
                tracer.AddAspNetCoreInstrumentation();
                tracer.AddHttpClientInstrumentation();
                tracer.AddOtlpExporter();
                tracer.AddProcessor<SignalRHubExporterProcessor>();
            });
    }
}
