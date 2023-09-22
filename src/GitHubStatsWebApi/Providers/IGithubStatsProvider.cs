using GitHubStatsWebApi.Models;

namespace GitHubStatsWebApi.Providers;

public interface IGithubStatsProvider
{
    Task<StatsResult?> GetStats(string username);
}

public interface IGitHubTypedStatsProvider<T> : IGithubStatsProvider
{
}