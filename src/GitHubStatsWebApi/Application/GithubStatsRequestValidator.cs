using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GitHubStatsWebApi.Models;

namespace GitHubStatsWebApi.Application;

public class GithubStatsRequestValidator : IValidator<StatsRequest>
{
    public ValidationResult? Validate(StatsRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Username))
        {
            return new ValidationResult("Missing username", new[] { nameof(model.Username) });
        }

        if (model.Username.Length < 4)
        {
            return new ValidationResult("Username should be longer than 3 characters", new[] { nameof(model.Username) });
        }

        if (!Regex.IsMatch(model.Username, @"^[\d\w]+$"))
        {
            return new ValidationResult("Username should be an alphanumeric string", new[] { nameof(model.Username) });
        }

        return ValidationResult.Success;
    }
}
