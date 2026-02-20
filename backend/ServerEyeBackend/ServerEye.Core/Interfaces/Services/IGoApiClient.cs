namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.GoApi;

public interface IGoApiClient
{
    public Task<GoApiMetricsResponse?> GetMetricsAsync(string serverId, DateTime start, DateTime endTime, string? granularity = null);
    public Task<GoApiMetricsResponse?> GetMetricsByKeyAsync(string serverKey, DateTime start, DateTime endTime, string? granularity = null);
    public Task<GoApiMetricsResponse?> GetRealtimeMetricsAsync(string serverId, TimeSpan? duration = null);
    public Task<GoApiMetricsResponse?> GetDashboardMetricsAsync(string serverId);
    public Task<GoApiServerInfo?> ValidateServerKeyAsync(string serverKey);
    public Task<GoApiServerInfo?> GetServerInfoAsync(string serverId);
    public Task<List<GoApiServerInfo>?> GetServersListAsync();
    public Task<GoApiStaticInfo?> GetStaticInfoAsync(string serverKey);
}
