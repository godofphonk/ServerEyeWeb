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
    
    // Source management methods
    public Task<GoApiSourceResponse?> AddServerSourceAsync(string serverId, string source);
    public Task<GoApiSourceResponse?> AddServerSourceByKeyAsync(string serverKey, string source);
    public Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersAsync(string serverId, GoApiSourceIdentifiersRequest request);
    public Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersByKeyAsync(string serverKey, GoApiSourceIdentifiersRequest request);
    
    // Server discovery methods
    public Task<List<GoApiServerInfo>?> FindServersByTelegramIdAsync(long telegramId);
}
