namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;
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

#pragma warning disable SA1204 // Static members should appear before non-static members
#pragma warning disable SA1202 // 'public' members should come before 'private' members
    private static MetricsSummary CalculateSummary(List<GoApiDataPoint> dataPoints)
#pragma warning restore SA1202 // 'public' members should come before 'private' members
#pragma warning restore SA1204 // Static members should appear before non-static members
    {
        if (dataPoints.Count == 0)
        {
            return new MetricsSummary();
        }

        return new MetricsSummary
        {
            AvgCpu = dataPoints.Average(dp => dp.CpuAvg),
            MaxCpu = dataPoints.Max(dp => dp.CpuMax),
            MinCpu = dataPoints.Min(dp => dp.CpuMin),
            AvgMemory = dataPoints.Average(dp => dp.MemoryAvg),
            MaxMemory = dataPoints.Max(dp => dp.MemoryMax),
            MinMemory = dataPoints.Min(dp => dp.MemoryMin),
            AvgDisk = dataPoints.Average(dp => dp.DiskAvg),
            MaxDisk = dataPoints.Max(dp => dp.DiskMax),
            TotalDataPoints = dataPoints.Count,
            TimeRange = dataPoints.Last().Timestamp - dataPoints.First().Timestamp
        };
    }

#pragma warning disable SA1202 // 'public' members should come before 'private' members
    public async Task<RawMetricsResponse> GetTieredMetricsByKeyAsync(Guid userId, string serverKey, DateTime start, DateTime endTime, string? granularity = null)
#pragma warning restore SA1202 // 'public' members should come before 'private' members
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        this.logger.LogInformation(
            "[PERF] GetTieredMetricsByKeyAsync started for server key {ServerKey} with Start={Start}, End={End}",
            serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
            start,
            endTime);

        try
        {
            // Validate server key and get server info for access check
            var accessCheckTime = System.Diagnostics.Stopwatch.StartNew();
            var serverInfo = await this.goApiClient.ValidateServerKeyAsync(serverKey ?? string.Empty);
#pragma warning disable IDE0270 // Simplify null check
            if (serverInfo is null)
#pragma warning restore IDE0270 // Simplify null check
            {
                throw new UnauthorizedAccessException("Invalid server key");
            }

            // Check user access to this server
            await this.ValidateAccessAsync(userId, serverInfo.ServerId);
            accessCheckTime.Stop();
            this.logger.LogInformation("[PERF] Access validation took {Ms}ms", accessCheckTime.ElapsedMilliseconds);

            // Generate cache key based on server key for tiered metrics
            var cacheKey = $"metrics:tiered:{serverKey}:{start:yyyyMMddHHmmss}:{endTime:yyyyMMddHHmmss}";
            var ttl = this.cacheService.CalculateTTL(start, endTime);

            var cacheTime = System.Diagnostics.Stopwatch.StartNew();
            var response = await this.cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    this.logger.LogInformation("[PERF] Cache miss - fetching tiered metrics from Go API");
                    var goApiTime = System.Diagnostics.Stopwatch.StartNew();
                    var goResponse = await this.goApiClient.GetTieredMetricsByKeyAsync(serverKey ?? string.Empty, start, endTime, granularity);
                    goApiTime.Stop();

                    if (goResponse == null)
                    {
                        this.logger.LogWarning("[PERF] Go API returned null after {Ms}ms", goApiTime.ElapsedMilliseconds);
                        throw new InvalidOperationException("Failed to retrieve tiered metrics from Go API");
                    }

                    if (goResponse.DataPoints == null || goResponse.DataPoints.Count == 0)
                    {
                        this.logger.LogWarning("[PERF] Go API returned empty tiered data after {Ms}ms", goApiTime.ElapsedMilliseconds);
                    }

                    this.logger.LogInformation("[PERF] Go API tiered response received in {GoApiMs}ms with {Points} data points", goApiTime.ElapsedMilliseconds, goResponse.DataPoints?.Count ?? 0);

                    // Return raw Go API response
                    var rawResponse = new RawMetricsResponse
                    {
                        ServerId = goResponse.ServerId,
                        ServerName = serverInfo.Hostname,
                        StartTime = goResponse.StartTime,
                        EndTime = goResponse.EndTime,
                        Granularity = goResponse.Granularity,
                        DataPoints = goResponse.DataPoints ?? new(),
                        TotalPoints = goResponse.TotalPoints,
                        Message = goResponse.Message ?? "Success",
                        Status = goResponse.Status?.Online == true ? "success" : "error",
                        Summary = CalculateSummary(goResponse.DataPoints ?? new()),
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
                    "Go API tiered message for server key {ServerKey}: {Message}",
                    serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                    response.Message?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            }

            stopwatch.Stop();
            this.logger.LogInformation(
                "[PERF] GetTieredMetricsByKeyAsync completed in {TotalMs}ms (access: {AccessMs}ms, cache: {CacheMs}ms) for server key {ServerKey}, data points: {Points}",
                stopwatch.ElapsedMilliseconds,
                accessCheckTime.ElapsedMilliseconds,
                cacheTime.ElapsedMilliseconds,
                serverKey,
                response.DataPoints?.Count ?? 0);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.logger.LogError(ex, "[PERF] Error in GetTieredMetricsByKeyAsync after {Ms}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

#pragma warning disable SA1202 // 'public' members should come before 'private' members
    public async Task<RawMetricsResponse> GetMetricsByKeyAsync(Guid userId, string serverKey, DateTime start, DateTime endTime, string? granularity = null)
#pragma warning restore SA1202 // 'public' members should come before 'private' members
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        this.logger.LogInformation(
            "[PERF] GetMetricsByKeyAsync started for server key {ServerKey} with Start={Start}, End={End}, Granularity={Granularity}",
            serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
            start,
            endTime,
            granularity?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

        try
        {
            // Validate server key and get server info for access check
            var accessCheckTime = System.Diagnostics.Stopwatch.StartNew();
            var serverInfo = await this.goApiClient.ValidateServerKeyAsync(serverKey ?? string.Empty);
#pragma warning disable IDE0270 // Simplify null check
            if (serverInfo is null)
#pragma warning restore IDE0270 // Simplify null check
            {
                throw new UnauthorizedAccessException("Invalid server key");
            }

            // Check user access to this server
            await this.ValidateAccessAsync(userId, serverInfo.ServerId);
            accessCheckTime.Stop();
            this.logger.LogInformation("[PERF] Access validation took {Ms}ms", accessCheckTime.ElapsedMilliseconds);

            // Generate cache key based on server key
            var cacheKey = $"metrics:by-key:{serverKey}:{start:yyyyMMddHHmmss}:{endTime:yyyyMMddHHmmss}:{granularity ?? "auto"}";
            var ttl = this.cacheService.CalculateTTL(start, endTime);

            var cacheTime = System.Diagnostics.Stopwatch.StartNew();
            var response = await this.cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    this.logger.LogInformation("[PERF] Cache miss - fetching from Go API by key");
                    var goApiTime = System.Diagnostics.Stopwatch.StartNew();
                    var goResponse = await this.goApiClient.GetMetricsByKeyAsync(serverKey ?? string.Empty, start, endTime, granularity);
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
                        ServerName = serverInfo.Hostname,
                        StartTime = goResponse.StartTime,
                        EndTime = goResponse.EndTime,
                        Granularity = goResponse.Granularity,
                        DataPoints = goResponse.DataPoints ?? new(),
                        TotalPoints = goResponse.TotalPoints,
                        Message = goResponse.Message ?? "Success",
                        Status = goResponse.Status?.Online == true ? "success" : "error",
                        Summary = CalculateSummary(goResponse.DataPoints ?? new()),
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
                    "Go API message for server key {ServerKey}: {Message}",
                    serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                    response.Message?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            }

            stopwatch.Stop();
            this.logger.LogInformation(
                "[PERF] GetMetricsByKeyAsync completed in {TotalMs}ms (access: {AccessMs}ms, cache: {CacheMs}ms) for server key {ServerKey}, granularity: {Granularity}, data points: {Points}",
                stopwatch.ElapsedMilliseconds,
                accessCheckTime.ElapsedMilliseconds,
                cacheTime.ElapsedMilliseconds,
                serverKey,
                granularity ?? "auto",
                response.DataPoints?.Count ?? 0);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.logger.LogError(ex, "[PERF] Error in GetMetricsByKeyAsync after {Ms}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<RawMetricsResponse> GetMetricsAsync(Guid userId, string serverId, DateTime? start, DateTime? endTime, string? granularity = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        this.logger.LogInformation(
            "[PERF] GetMetricsAsync started for server {ServerId} with Start={Start}, End={End}, Granularity={Granularity}",
            serverId?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
            start,
            endTime,
            granularity?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

        var accessCheckTime = System.Diagnostics.Stopwatch.StartNew();
        await this.ValidateAccessAsync(userId, serverId ?? string.Empty);
        accessCheckTime.Stop();
        this.logger.LogInformation("[PERF] Access validation took {Ms}ms", accessCheckTime.ElapsedMilliseconds);

        var dbQueryTime = System.Diagnostics.Stopwatch.StartNew();
        var server = await this.serverRepository.GetByServerIdAsync(serverId ?? string.Empty) ?? throw new InvalidOperationException("Server not found");
        dbQueryTime.Stop();
        this.logger.LogInformation("[PERF] Database query took {Ms}ms", dbQueryTime.ElapsedMilliseconds);

        // Handle null parameters - use default behavior
        if (!start.HasValue && !endTime.HasValue)
        {
            this.logger.LogInformation("No time range provided, using default behavior");
            endTime = DateTime.UtcNow;
            start = endTime.Value.AddMinutes(-5);
            granularity = "1m";
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
                var goResponse = await this.goApiClient.GetMetricsByKeyAsync(server.ServerKey, start!.Value, endTime!.Value, granularity);
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
                    Status = goResponse.Status?.Online == true ? "success" : "error",
                    Summary = CalculateSummary(goResponse.DataPoints ?? new()),
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

    private async Task ValidateAccessAsync(Guid userId, string serverId)
    {
        var hasAccess = await this.accessRepository.HasAccessAsync(userId, serverId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("You don't have access to this server");
        }
    }
}
