namespace GitHubStatsWebApi.Models;

public record StatsResult(string Name, string Location, int Public_Repos, int Followers, DateTime? Updated_At);