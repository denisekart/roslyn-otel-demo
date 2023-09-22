using Microsoft.AspNetCore.SignalR;

namespace GitHubStatsWebApi.Telemetry;

public sealed class TelemetryHub : Hub<ITelemetryClient>
{
    public async Task Transmit(TelemetryData telemetry) 
        => await Clients.All.ReceiveTelemetry(telemetry);
}