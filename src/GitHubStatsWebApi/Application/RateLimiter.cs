namespace GitHubStatsWebApi.Application;

public class RateLimiter(int timeoutInSeconds)
{
    private DateTime _lastAccess = DateTime.MinValue;
    readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    public bool CanAccess()
    {
        var currentTime = DateTime.Now;
        
        try
        {
            _semaphore.Wait();
            if (currentTime > _lastAccess.AddSeconds(timeoutInSeconds))
            {
                _lastAccess = currentTime;
                return true;
            }
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
