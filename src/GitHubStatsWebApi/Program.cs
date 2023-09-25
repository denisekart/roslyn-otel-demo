using GitHubStatsWebApi;
using GitHubStatsWebApi.Application;
using GitHubStatsWebApi.Extensions;
using GitHubStatsWebApi.Models;
using GitHubStatsWebApi.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddTelemetry();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices();

var app = builder.Build(enableAutoInstrumentation: true);

app.ConfigureRequestPipelineDefaults();

// API routes
var api = app.MapGroup("api");
api.MapGet("/stats/{username?}", HandleGetStats).WithDescription("Returns Github statistics for user");

app.ConfigureUiPipeline();

app.Run();

static async Task<IResult> HandleGetStats(string? username, IGithubStatsService statsService)
{
    var stats = await statsService.GetStatsForUser(new StatsRequest(username));
    return TypedResults.Json(stats);
}
