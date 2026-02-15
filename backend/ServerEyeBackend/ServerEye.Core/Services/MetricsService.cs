namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.DTOs.WebSocket;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class MetricsService : IMetricsService
{
    private readonly IGoApiClient goApiClient;
    private readonly IMetricsCacheService cacheService;
    private readonly IMonitoredServerRepository serverRepository;
    private readonly IUserServerAccessRepository accessRepository;
    private readonly IWebSocketTokenService webSocketTokenService;
    private readonly ILogger<MetricsService> logger;

    public MetricsService(
        IGoApiClient goApiClient,
        IMetricsCacheService cacheService,
        IMonitoredServerRepository serverRepository,
        IUserServerAccessRepository accessRepository,
        IWebSocketTokenService webSocketTokenService,
        ILogger<MetricsService> logger)
    {
        this.goApiClient = goApiClient;
        this.cacheService = cacheService;
        this.serverRepository = serverRepository;
        this.accessRepository = accessRepository;
        this.webSocketTokenService = webSocketTokenService;
        this.logger = logger;
    }

    public async Task<MetricsResponse> GetMetricsAsync(Guid userId, string serverId, DateTime start, DateTime endTime, string? granularity = null)
    {
        await this.ValidateAccessAsync(userId, serverId);

        var server = await this.serverRepository.GetByServerIdAsync(serverId) ?? throw new InvalidOperationException("Server not found");

        var cacheKey = $"metrics:{serverId}:{start:yyyyMMddHHmmss}:{endTime:yyyyMMddHHmmss}:{granularity ?? "auto"}";
        var ttl = this.cacheService.CalculateTTL(start, endTime);

        var response = await this.cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var goResponse = await this.goApiClient.GetMetricsAsync(serverId, start, endTime, granularity) ?? throw new InvalidOperationException("Failed to retrieve metrics from Go API");
                return MetricsMapper.MapToResponse(goResponse, server, false);
            },
            ttl);

        ArgumentNullException.ThrowIfNull(response);

        response.IsCached = true;
        response.CachedAt = DateTime.UtcNow;

        this.logger.LogInformation(
            "Retrieved metrics for server {ServerId} from {Start} to {End}, granularity: {Granularity}, cached: {IsCached}",
            serverId,
            start,
            endTime,
            granularity ?? "auto",
            response.IsCached);

        return response;
    }

    public async Task<MetricsResponse> GetRealtimeMetricsAsync(Guid userId, string serverId, TimeSpan? duration = null)
    {
        await this.ValidateAccessAsync(userId, serverId);

        var server = await this.serverRepository.GetByServerIdAsync(serverId) ?? throw new InvalidOperationException("Server not found");

        var goResponse = await this.goApiClient.GetRealtimeMetricsAsync(serverId, duration) ?? throw new InvalidOperationException("Failed to retrieve realtime metrics from Go API");

        var response = MetricsMapper.MapToResponse(goResponse, server, false);

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

        this.logger.LogInformation("Retrieved dashboard metrics for server {ServerId}", serverId);

        return response;
    }

    public async Task<WebSocketTokenResponse> GenerateWebSocketTokenAsync(Guid userId, string serverId)
    {
        await this.ValidateAccessAsync(userId, serverId);

        var ttl = TimeSpan.FromMinutes(30);
        var token = this.webSocketTokenService.GenerateToken(userId, serverId, ttl);
        var expiresAt = DateTime.UtcNow.Add(ttl);

        var wsUrl = new Uri($"ws://localhost:8080/ws?token={token}");

        this.logger.LogInformation("Generated WebSocket token for user {UserId} and server {ServerId}", userId, serverId);

        return new WebSocketTokenResponse
        {
            Token = token,
            WsUrl = wsUrl,
            ExpiresAt = expiresAt
        };
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
