namespace ServerEye.API.Configuration.Extensions;

using ServerEye.Core.Configuration;

/// <summary>
/// Caching configuration setup.
/// </summary>
public static class CachingSetup
{
    /// <summary>
    /// Adds Redis caching and cache-related services.
    /// </summary>
    public static IServiceCollection AddCachingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection("RedisSettings").Get<RedisSettings>() 
            ?? new RedisSettings();

        var cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>() 
            ?? new CacheSettings();

        // Configure Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["REDIS_CONNECTION_STRING"] 
                ?? redisSettings.ConnectionString;
            options.InstanceName = redisSettings.InstanceName;
        });

        // Register cache settings
        services.AddSingleton(cacheSettings);

        // Add Redis health check
        services.AddHealthChecks()
            .AddRedis(
                redisConnectionString: configuration["REDIS_CONNECTION_STRING"] 
                    ?? redisSettings.ConnectionString,
                name: "redis",
                tags: ["cache", "redis", "ready"]);

        return services;
    }
}
