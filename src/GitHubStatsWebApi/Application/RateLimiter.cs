namespace GitHubStatsWebApi.Application;

/// <summary>
/// A simple implementation of a rate limiter based on an elapsed time
/// </summary>
/// <param name="timeoutInSeconds">The timeout in seconds after which the resource can be accessed again</param>
public class RateLimiter(int timeoutInSeconds)
{
    private DateTime _lastAccess = DateTime.MinValue;
    readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    /// <summary>
    /// Returns true if the resource can be accessed
    /// </summary>
    public bool CanAccess()
    {
        var currentTime = DateTime.Now;

        try
        {
            _semaphore.Wait();
            if (currentTime <= _lastAccess.AddSeconds(timeoutInSeconds))
            {
                return false;
            }

            _lastAccess = currentTime;
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
