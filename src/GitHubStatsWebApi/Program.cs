using GitHubStatsWebApi;
using GitHubStatsWebApi.Application;
using GitHubStatsWebApi.Extensions;
using GitHubStatsWebApi.Models;
using GitHubStatsWebApi.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddTelemetry();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices();

var app = builder.Build(); 

app.ConfigureRequestPipelineDefaults();

// API routes
var api = app.MapGroup("api");
api.MapGet("/stats/{username?}",
        async (string? username, IGithubStatsService statsService) =>
        {
            var stats = await statsService.GetStatsForUser(new StatsRequest(username));
            return TypedResults.Json(stats);
        })
    .WithDescription("Returns Github statistics for username provided in the request");

app.ConfigureUiPipeline();

app.Run();
