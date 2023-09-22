
using GitHubStatsWebApi.Application;
using GitHubStatsWebApi.Models;

namespace GitHubStatsWebApi.Providers;

public class GithubRateLimitingStatsProvider(IGitHubTypedStatsProvider<GithubApiStatsProvider> baseProvider, RateLimiter rateLimiter) : IGitHubTypedStatsProvider<GithubRateLimitingStatsProvider>
{   
    public async Task<StatsResult?> GetStats(string username)
    {
        if (!rateLimiter.CanAccess())
        {
            throw new InvalidOperationException("You are being rate limited. Please try again later.");
        }

        return await baseProvider.GetStats(username);
    }
}