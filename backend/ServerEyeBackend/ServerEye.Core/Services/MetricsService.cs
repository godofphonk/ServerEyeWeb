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

    public async Task<RawMetricsResponse> GetMetricsAsync(Guid userId, string serverId, DateTime? start, DateTime? endTime, string? granularity = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        this.logger.LogInformation(
            "[PERF] GetMetricsAsync started for server {ServerId} with Start={Start}, End={End}, Granularity={Granularity}",
            serverId,
            start,
            endTime,
            granularity);

        var accessCheckTime = System.Diagnostics.Stopwatch.StartNew();
        await this.ValidateAccessAsync(userId, serverId);
        accessCheckTime.Stop();
        this.logger.LogInformation("[PERF] Access validation took {Ms}ms", accessCheckTime.ElapsedMilliseconds);

        var dbQueryTime = System.Diagnostics.Stopwatch.StartNew();
        var server = await this.serverRepository.GetByServerIdAsync(serverId) ?? throw new InvalidOperationException("Server not found");
        dbQueryTime.Stop();
        this.logger.LogInformation("[PERF] Database query took {Ms}ms", dbQueryTime.ElapsedMilliseconds);

        // Handle null parameters - use default behavior for dashboard
        if (!start.HasValue && !endTime.HasValue)
        {
            this.logger.LogInformation("No time range provided, using dashboard default behavior");
            return await this.GetDashboardMetricsAsync(userId, serverId);
        }

        var cacheKey = $"metrics:{serverId}:{start!.Value:yyyyMMddHHmmss}:{endTime!.Value:yyyyMMddHHmmss}:{granularity ?? "auto"}";
        var ttl = this.cacheService.CalculateTTL(start!.Value, endTime!.Value);

        var cacheTime = System.Diagnostics.Stopwatch.StartNew();
        var response = await this.cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                this.logger.LogInformation("[PERF] Cache miss - fetching from Go API");
                var goApiTime = System.Diagnostics.Stopwatch.StartNew();
                var goResponse = await this.goApiClient.GetMetricsAsync(serverId, start!.Value, endTime!.Value, granularity);
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
                
                // Return raw Go API response without mapping
                var rawResponse = new RawMetricsResponse
                {
                    ServerId = goResponse.ServerId,
                    ServerName = server.Hostname,
                    StartTime = goResponse.StartTime,
                    EndTime = goResponse.EndTime,
                    Granularity = goResponse.Granularity,
                    DataPoints = goResponse.DataPoints ?? new(),
                    TotalPoints = goResponse.TotalPoints,
                    Message = goResponse.Message,
                    Status = goResponse.Status,
                    TemperatureDetails = goResponse.TemperatureDetails,
                    NetworkDetails = goResponse.NetworkDetails,
                    DiskDetails = goResponse.DiskDetails,
                    IsCached = false,
                    CachedAt = null
                };
                
                return rawResponse;
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

    public async Task<RawMetricsResponse> GetRealtimeMetricsAsync(Guid userId, string serverId, TimeSpan? duration = null)
    {
        var actualDuration = duration ?? TimeSpan.FromMinutes(5);
        var end = DateTime.UtcNow;
        var start = end - actualDuration;
        
        return await this.GetMetricsAsync(userId, serverId, start, end, "1m");
    }

    public async Task<RawMetricsResponse> GetDashboardMetricsAsync(Guid userId, string serverId)
    {
        var end = DateTime.UtcNow;
        var start = end - TimeSpan.FromMinutes(5);
        
        return await this.GetMetricsAsync(userId, serverId, start, end, "1m");
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
