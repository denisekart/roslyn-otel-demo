using System.ComponentModel.DataAnnotations;
using GitHubStatsWebApi.Extensions;
using GitHubStatsWebApi.Models;
using GitHubStatsWebApi.Providers;

namespace GitHubStatsWebApi.Application;

public class GithubStatsService(IValidator<StatsRequest> requestValidator, IGithubStatsProvider statsProvider) : IGithubStatsService
{
    public async Task<StatsResponse?> GetStatsForUser(StatsRequest request)
    {
        var validationResult = requestValidator.Validate(request);
        if (validationResult != ValidationResult.Success)
        {
            throw new ValidationException(validationResult!.ToString());
        }

        var stats = await statsProvider.GetStats(request.Username!);

        return stats.ConvertToResponse();
    }
}