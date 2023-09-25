using GitHubStatsWebApi.Models;

namespace GitHubStatsWebApi.Providers;

public class GithubStaleLocalStatsProvider(IHostEnvironment hostEnvironment, IGitHubTypedStatsProvider<GithubRateLimitingStatsProvider> baseProvider)
    : IGitHubTypedStatsProvider<GithubStaleLocalStatsProvider>
{
    private readonly LocalStatsFileRepository _fileRepository = new(hostEnvironment.ContentRootFileProvider);

    public async Task<StatsResult?> GetStats(string username)
    {
        try
        {
            var result = await baseProvider.GetStats(username);
            if (result is not null)
            {
                await _fileRepository.SetStats(username, result);
            }
        }
        catch
        {
            // catch all exceptions and attempt to recover with locally stored data
        }

        return await _fileRepository.GetStats(username);
    }
}