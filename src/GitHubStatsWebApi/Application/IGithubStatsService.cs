using GitHubStatsWebApi.Models;

namespace GitHubStatsWebApi.Application;

public interface IGithubStatsService
{
    Task<StatsResponse?> GetStatsForUser(StatsRequest request);
}
