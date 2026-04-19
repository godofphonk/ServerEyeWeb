namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Helpers;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Interfaces.Services.Billing;

public class MetricsService : IMetricsService
{
    private readonly IGoApiClient goApiClient;
    private readonly IMetricsCacheService cacheService;
    private readonly IUserServerAccessRepository accessRepository;
    private readonly IPlanLimitsService planLimitsService;
    private readonly ILogger<MetricsService> logger;

    public MetricsService(
        IGoApiClient goApiClient,
        IMetricsCacheService cacheService,
        IUserServerAccessRepository accessRepository,
        IPlanLimitsService planLimitsService,
        ILogger<MetricsService> logger)
    {
        this.goApiClient = goApiClient;
        this.cacheService = cacheService;
        this.accessRepository = accessRepository;
        this.planLimitsService = planLimitsService;
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

            // Clamp start time to retention limit
            var retentionDays = await planLimitsService.GetMetricsRetentionDaysAsync(userId);
            var maxStartDate = DateTime.UtcNow.AddDays(-retentionDays);
            var retentionLimited = start < maxStartDate;
            if (retentionLimited)
            {
                start = maxStartDate;
                this.logger.LogInformation("[RETENTION] Start time clamped to {MaxStartDate} due to plan limit of {RetentionDays} days", maxStartDate, retentionDays);
            }

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
                        CachedAt = null,
                        RetentionLimited = retentionLimited
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
                LogSanitizer.MaskServerKey(serverKey),
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

    private async Task ValidateAccessAsync(Guid userId, string serverId)
    {
        var hasAccess = await this.accessRepository.HasAccessAsync(userId, serverId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("You don't have access to this server");
        }
    }
}
