using GitHubStatsWebApi.Models;

namespace GitHubStatsWebApi.Extensions;

public static class GithubResponseConversionExtensions
{
    public static StatsResponse ConvertToResponse(this StatsResult? result)
    {
        if (result is null)
        {
            return new StatsResponse("<unknown>", "<unknown>", 0, 0, null);
        }

        return new(
            Name: result.Name,
            GeoLocation: result.Location,
            PublicRepositories: result.Public_Repos,
            NumberOfFollowers: result.Followers,
            LastActivity: result.Updated_At);
    }
}
