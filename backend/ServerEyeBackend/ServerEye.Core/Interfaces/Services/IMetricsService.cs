namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.DTOs.WebSocket;

public interface IMetricsService
{
    public Task<MetricsResponse> GetMetricsAsync(Guid userId, string serverId, DateTime start, DateTime endTime, string? granularity = null);
    public Task<MetricsResponse> GetRealtimeMetricsAsync(Guid userId, string serverId, TimeSpan? duration = null);
    public Task<MetricsResponse> GetDashboardMetricsAsync(Guid userId, string serverId);
    public Task<WebSocketTokenResponse> GenerateWebSocketTokenAsync(Guid userId, string serverId);
}
