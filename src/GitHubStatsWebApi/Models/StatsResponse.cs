namespace GitHubStatsWebApi.Models;

public record StatsResponse(string? Name, string? GeoLocation, int PublicRepositories, int NumberOfFollowers, DateTime? LastActivity);