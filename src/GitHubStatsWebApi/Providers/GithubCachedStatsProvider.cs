using GitHubStatsWebApi.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GitHubStatsWebApi.Providers;

public class GithubCachedStatsProvider(IGitHubTypedStatsProvider<GithubStaleLocalStatsProvider> baseProvider, IMemoryCache cache) : IGitHubTypedStatsProvider<GithubCachedStatsProvider>
{
    private readonly TimeSpan _relativeExpirationSpan = TimeSpan.FromSeconds(10);

    Task<StatsResult?> GetOrAddStats(string username, Func<Task<StatsResult?>> addFactory)
    {
        return cache.GetOrCreateAsync(username,
            async entry =>
            {
                var value = await addFactory();
                entry.SetAbsoluteExpiration(_relativeExpirationSpan);

                return value;
            });
    }

    public Task<StatsResult?> GetStats(string username)
    {
        return GetOrAddStats(username, async () => await baseProvider.GetStats(username));
    }
}