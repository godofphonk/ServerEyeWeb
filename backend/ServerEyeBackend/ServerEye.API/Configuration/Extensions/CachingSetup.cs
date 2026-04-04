namespace ServerEye.API.Configuration.Extensions;

using ServerEye.Core.Configuration;
using StackExchange.Redis;

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

        var connectionString = configuration["REDIS_CONNECTION_STRING"]
            ?? redisSettings.ConnectionString;

        // Register IConnectionMultiplexer as a singleton so that OpenTelemetry Redis
        // instrumentation (and other consumers) can resolve it from DI.
        // AbortOnConnectFail = false allows the app to start even when Redis is unavailable.
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        // Configure Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = redisSettings.InstanceName;
        });

        // Register cache settings
        services.AddSingleton(cacheSettings);

        // Add Redis health check
        services.AddHealthChecks()
            .AddRedis(
                redisConnectionString: connectionString,
                name: "redis",
                tags: ["cache", "redis", "ready"]);

        return services;
    }
}
