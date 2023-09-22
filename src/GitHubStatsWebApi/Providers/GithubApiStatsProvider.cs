
using GitHubStatsWebApi.Models;

namespace GitHubStatsWebApi.Providers;

public class GithubApiStatsProvider(IHttpClientFactory httpClientFactory) : IGitHubTypedStatsProvider<GithubApiStatsProvider>
{
    public const string GithubApiClientName = "GithubApi";
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<StatsResult?> GetStats(string username)
    {
        var client = _httpClientFactory.CreateClient(GithubApiClientName);
        var response = await client.GetFromJsonAsync<StatsResult>($"/users/{username}");

        if (response is not null)
        {
            return response with
            {
                // this line makes me look good
                Followers = response.Followers + Random.Shared.Next(1, 100)
            };
        }
        return response;
    }
}