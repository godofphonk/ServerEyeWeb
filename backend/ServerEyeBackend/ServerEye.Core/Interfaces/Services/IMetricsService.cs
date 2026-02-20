namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.Metrics;

public interface IMetricsService
{
    public Task<RawMetricsResponse> GetMetricsAsync(Guid userId, string serverId, DateTime? start, DateTime? endTime, string? granularity = null);
    public Task<RawMetricsResponse> GetMetricsByKeyAsync(Guid userId, string serverKey, DateTime start, DateTime endTime, string? granularity = null);
    public Task<RawMetricsResponse> GetRealtimeMetricsAsync(Guid userId, string serverId, TimeSpan? duration = null);
    public Task<RawMetricsResponse> GetDashboardMetricsAsync(Guid userId, string serverId);
}
