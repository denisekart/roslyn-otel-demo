using System.ComponentModel.DataAnnotations;

namespace GitHubStatsWebApi.Application;

public interface IValidator<T>
{
    ValidationResult? Validate(T model);
}
