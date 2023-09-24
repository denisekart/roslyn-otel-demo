using System.Text.Json;
using GitHubStatsWebApi.Models;
using Microsoft.Extensions.FileProviders;

namespace GitHubStatsWebApi.Providers;

public class GithubStaleLocalStatsProvider(IHostEnvironment hostEnvironment, IGitHubTypedStatsProvider<GithubRateLimitingStatsProvider> baseProvider) : IGitHubTypedStatsProvider<GithubStaleLocalStatsProvider>
{
    private readonly IFileProvider _fileProvider = hostEnvironment.ContentRootFileProvider;

    public async Task<StatsResult?> GetStats(string username)
    {
        try
        {
            var result = await baseProvider.GetStats(username);
            if (result is not null)
            {
                await SetStats(username, result);
            }
        }
        catch
        {
            // catch all exceptions and attempt to recover with locally stored data
        }

        var filename = ToFilename(username);
        var fileInfo = _fileProvider.GetFileInfo(filename);

        if (!fileInfo.Exists)
        {
            return null;
        }

        await using var readStream = fileInfo.CreateReadStream();
        var fileContents = await JsonSerializer.DeserializeAsync<StatsResult>(readStream);

        return fileContents;
    }


    public async Task SetStats(string username, StatsResult stats)
    {
        var filename = ToFilename(username);
        var fullFilePath = _fileProvider.GetFileInfo(filename).PhysicalPath!;
        
        await using var writeStream = new FileStream(fullFilePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(writeStream, stats);
    }

    private static string ToFilename(string username) => $"{username}_local_github_stats.json";
}