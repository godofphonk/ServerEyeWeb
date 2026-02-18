namespace ServerEye.Core.Interfaces.Services;

public interface IMetricsCacheService
{
    public Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
        where T : class;

    public Task<T?> GetAsync<T>(string key)
        where T : class;

    public Task SetAsync<T>(string key, T value, TimeSpan ttl)
        where T : class;

    public Task RemoveAsync(string key);

    public TimeSpan CalculateTTL(DateTime start, DateTime endTime);
}
