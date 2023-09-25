using System.Text.Json;
using GitHubStatsWebApi.Models;
using Microsoft.Extensions.FileProviders;

namespace GitHubStatsWebApi.Providers;

public class LocalStatsFileRepository(IFileProvider fileProvider)
{
    public async Task<StatsResult?> GetStats(string username)
    {
        var filename = ToFilename(username);
        var fileInfo = fileProvider.GetFileInfo(filename);

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
        var fullFilePath = fileProvider.GetFileInfo(filename).PhysicalPath!;
        
        await using var writeStream = new FileStream(fullFilePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(writeStream, stats);
    }
    
    private static string ToFilename(string username) => $"{username}_local_github_stats.json";
}
