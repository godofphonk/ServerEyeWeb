namespace ServerEye.Infrastructure.Caching;

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.Interfaces.Services;

public class MetricsCacheService : IMetricsCacheService
{
    private readonly IDistributedCache cache;
    private readonly CacheSettings settings;
    private readonly ILogger<MetricsCacheService> logger;

    public MetricsCacheService(IDistributedCache cache, CacheSettings settings, ILogger<MetricsCacheService> logger)
    {
        this.cache = cache;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
        where T : class
    {
        var cached = await this.GetAsync<T>(key);
        if (cached != null)
        {
            this.logger.LogDebug("Cache hit for key: {Key}", key);
            return cached;
        }

        this.logger.LogDebug("Cache miss for key: {Key}", key);
        var data = await factory();

        if (data != null)
        {
            await this.SetAsync(key, data, ttl);
        }

        return data;
    }

    public async Task<T?> GetAsync<T>(string key)
        where T : class
    {
        try
        {
            var cached = await this.cache.GetStringAsync(key);
            if (cached == null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(cached);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting cache key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
        where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            var serialized = JsonSerializer.Serialize(value);
            await this.cache.SetStringAsync(key, serialized, options);

            this.logger.LogDebug("Cached key: {Key} with TTL: {TTL}", key, ttl);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error setting cache key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await this.cache.RemoveAsync(key);
            this.logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public TimeSpan CalculateTTL(DateTime start, DateTime endTime)
    {
        var duration = endTime - start;

        return duration.TotalHours switch
        {
            <= 1 => this.settings.LiveMetrics,
            <= 3 => this.settings.HourMetrics,
            <= 24 => this.settings.DayMetrics,
            _ => this.settings.MonthMetrics
        };
    }
}
