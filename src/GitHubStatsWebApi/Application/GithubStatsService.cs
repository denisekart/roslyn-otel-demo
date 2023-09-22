using System.ComponentModel.DataAnnotations;
using GitHubStatsWebApi.Extensions;
using GitHubStatsWebApi.Models;
using GitHubStatsWebApi.Providers;

namespace GitHubStatsWebApi.Application;

public class GithubStatsService(IValidator<StatsRequest> requestValidator, IGithubStatsProvider statsProvider) : IGithubStatsService
{
    private readonly IValidator<StatsRequest> _requestValidator = requestValidator;
    private readonly IGithubStatsProvider _statsProvider = statsProvider;

    public async Task<StatsResponse?> GetStatsForUser(StatsRequest request)
    {
        var validationResult = _requestValidator.Validate(request);
        if (validationResult != ValidationResult.Success)
        {
            throw new ValidationException(validationResult!.ToString());
        }

        var stats = await _statsProvider.GetStats(request.Username!);

        return stats.ConvertToResponse();
    }
}