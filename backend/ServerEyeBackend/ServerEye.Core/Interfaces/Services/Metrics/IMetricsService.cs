namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.Metrics;

public interface IMetricsService
{
    public Task<RawMetricsResponse> GetMetricsByKeyAsync(Guid userId, string serverKey, DateTime start, DateTime endTime, string? granularity = null);
    public Task<RawMetricsResponse> GetTieredMetricsByKeyAsync(Guid userId, string serverKey, DateTime start, DateTime endTime);
}
