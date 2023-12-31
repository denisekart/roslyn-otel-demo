using System.Net.Http.Headers;
using GitHubStatsWebApi.Application;
using GitHubStatsWebApi.Models;
using GitHubStatsWebApi.Providers;
using GitHubStatsWebApi.Telemetry;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace GitHubStatsWebApi.Extensions;

public static class ApplicationServicesExtensions
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache(opts => opts.ExpirationScanFrequency = TimeSpan.FromSeconds(2));
        services.AddOptions<GithubOptions>().Bind(configuration.GetSection(GithubOptions.Section));

        services.AddSingleton(_ => new RateLimiter(timeoutInSeconds: 30));

        services.AddTransient<IGitHubTypedStatsProvider<GithubApiStatsProvider>, GithubApiStatsProvider>();
        services.AddTransient<IGitHubTypedStatsProvider<GithubRateLimitingStatsProvider>, GithubRateLimitingStatsProvider>();
        services.AddTransient<IGitHubTypedStatsProvider<GithubStaleLocalStatsProvider>, GithubStaleLocalStatsProvider>();
        services.AddTransient<IGithubStatsProvider, GithubCachedStatsProvider>();

        services.AddTransient<IGithubStatsService, GithubStatsService>();
        services.AddTransient<IValidator<StatsRequest>, GithubStatsRequestValidator>();
        
        services.AddHttpClient(GithubApiStatsProvider.GithubApiClientName,
            (provider, client) =>
            {
                var accessToken = provider.GetRequiredService<IOptions<GithubOptions>>().Value.AccessToken;
                client.BaseAddress = new Uri("https://api.github.com/");

                client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                client.DefaultRequestHeaders.Authorization = new("Token", accessToken);
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DecoratorsWebApi", "1.0"));
            });
    }

    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddSignalR();
        services.AddResponseCompression(opts => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" }));
    }

    public static void ConfigureRequestPipelineDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseResponseCompression();
        }
    }
    
    public static void ConfigureUiPipeline(this WebApplication app)
    {
        // Real time communication 
        app.MapHub<TelemetryHub>("/telemetryhub");

        // UI routes
        app.UseBlazorFrameworkFiles();
        app.MapFallbackToFile("index.html");
        app.UseStaticFiles();
    }
}
