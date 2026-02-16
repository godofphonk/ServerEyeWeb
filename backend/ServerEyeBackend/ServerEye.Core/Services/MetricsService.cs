namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class MetricsService : IMetricsService
{
    private readonly IGoApiClient goApiClient;
    private readonly IMetricsCacheService cacheService;
    private readonly IMonitoredServerRepository serverRepository;
    private readonly IUserServerAccessRepository accessRepository;
    private readonly ILogger<MetricsService> logger;

    public MetricsService(
        IGoApiClient goApiClient,
        IMetricsCacheService cacheService,
        IMonitoredServerRepository serverRepository,
        IUserServerAccessRepository accessRepository,
        ILogger<MetricsService> logger)
    {
        this.goApiClient = goApiClient;
        this.cacheService = cacheService;
        this.serverRepository = serverRepository;
        this.accessRepository = accessRepository;
        this.logger = logger;
    }

    public async Task<MetricsResponse> GetMetricsAsync(Guid userId, string serverId, DateTime start, DateTime endTime, string? granularity = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        this.logger.LogInformation("[PERF] GetMetricsAsync started for server {ServerId}", serverId);

        var accessCheckTime = System.Diagnostics.Stopwatch.StartNew();
        await this.ValidateAccessAsync(userId, serverId);
        accessCheckTime.Stop();
        this.logger.LogInformation("[PERF] Access validation took {Ms}ms", accessCheckTime.ElapsedMilliseconds);

        var dbQueryTime = System.Diagnostics.Stopwatch.StartNew();
        var server = await this.serverRepository.GetByServerIdAsync(serverId) ?? throw new InvalidOperationException("Server not found");
        dbQueryTime.Stop();
        this.logger.LogInformation("[PERF] Database query took {Ms}ms", dbQueryTime.ElapsedMilliseconds);

        var cacheKey = $"metrics:{serverId}:{start:yyyyMMddHHmmss}:{endTime:yyyyMMddHHmmss}:{granularity ?? "auto"}";
        var ttl = this.cacheService.CalculateTTL(start, endTime);

        var cacheTime = System.Diagnostics.Stopwatch.StartNew();
        var response = await this.cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                this.logger.LogInformation("[PERF] Cache miss - fetching from Go API");
                var goApiTime = System.Diagnostics.Stopwatch.StartNew();
                var goResponse = await this.goApiClient.GetMetricsAsync(serverId, start, endTime, granularity);
                goApiTime.Stop();
                
                if (goResponse == null)
                {
                    this.logger.LogWarning("[PERF] Go API returned null after {Ms}ms", goApiTime.ElapsedMilliseconds);
                    throw new InvalidOperationException("Failed to retrieve metrics from Go API");
                }
                
                if (goResponse.DataPoints == null || goResponse.DataPoints.Count == 0)
                {
                    this.logger.LogWarning("[PERF] Go API returned empty data after {Ms}ms - caching with short TTL", goApiTime.ElapsedMilliseconds);
                }
                
                var mapTime = System.Diagnostics.Stopwatch.StartNew();
                var mapped = MetricsMapper.MapToResponse(goResponse, server, false);
                mapTime.Stop();
                this.logger.LogInformation("[PERF] Mapping took {Ms}ms", mapTime.ElapsedMilliseconds);
                
                return mapped;
            },
            ttl);
        cacheTime.Stop();

        ArgumentNullException.ThrowIfNull(response);

        response.IsCached = true;
        response.CachedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(response.Message))
        {
            this.logger.LogInformation(
                "Go API message for server {ServerId}: {Message}",
                serverId,
                response.Message);
        }

        stopwatch.Stop();
        this.logger.LogInformation(
            "[PERF] GetMetricsAsync completed in {TotalMs}ms (access: {AccessMs}ms, db: {DbMs}ms, cache: {CacheMs}ms) for server {ServerId}, granularity: {Granularity}, data points: {Points}",
            stopwatch.ElapsedMilliseconds,
            accessCheckTime.ElapsedMilliseconds,
            dbQueryTime.ElapsedMilliseconds,
            cacheTime.ElapsedMilliseconds,
            serverId,
            granularity ?? "auto",
            response.DataPoints?.Count ?? 0);

        return response;
    }

    public async Task<MetricsResponse> GetRealtimeMetricsAsync(Guid userId, string serverId, TimeSpan? duration = null)
    {
        await this.ValidateAccessAsync(userId, serverId);

        var server = await this.serverRepository.GetByServerIdAsync(serverId) ?? throw new InvalidOperationException("Server not found");

        var goResponse = await this.goApiClient.GetRealtimeMetricsAsync(serverId, duration) ?? throw new InvalidOperationException("Failed to retrieve realtime metrics from Go API");

        var response = MetricsMapper.MapToResponse(goResponse, server, false);

        if (!string.IsNullOrEmpty(response.Message))
        {
            this.logger.LogInformation(
                "Go API message for server {ServerId}: {Message}",
                serverId,
                response.Message);
        }

        this.logger.LogInformation(
            "Retrieved realtime metrics for server {ServerId}, duration: {Duration}",
            serverId,
            duration?.ToString() ?? "default");

        return response;
    }

    public async Task<MetricsResponse> GetDashboardMetricsAsync(Guid userId, string serverId)
    {
        await this.ValidateAccessAsync(userId, serverId);

        var server = await this.serverRepository.GetByServerIdAsync(serverId) ?? throw new InvalidOperationException("Server not found");

        var cacheKey = $"dashboard:{serverId}";
        var ttl = TimeSpan.FromMinutes(5);

        var response = await this.cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var goResponse = await this.goApiClient.GetDashboardMetricsAsync(serverId) ?? throw new InvalidOperationException("Failed to retrieve dashboard metrics from Go API");
                return MetricsMapper.MapToResponse(goResponse, server, false);
            },
            ttl);

        ArgumentNullException.ThrowIfNull(response);

        response.IsCached = true;
        response.CachedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(response.Message))
        {
            this.logger.LogInformation(
                "Go API message for server {ServerId}: {Message}",
                serverId,
                response.Message);
        }

        this.logger.LogInformation("Retrieved dashboard metrics for server {ServerId}", serverId);

        return response;
    }

    private async Task ValidateAccessAsync(Guid userId, string serverId)
    {
        var hasAccess = await this.accessRepository.HasAccessAsync(userId, serverId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("You don't have access to this server");
        }
    }
}
