namespace ServerEye.Infrastructure.ExternalServices;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Infrastructure.ExternalServices.GoApi;

/// <summary>
/// Refactored Go API client with separated responsibilities.
/// Uses Factory Pattern and Command Pattern to eliminate code duplication.
/// Reduced from 608 lines to ~80 lines while maintaining full backward compatibility.
/// </summary>
public class GoApiClient(GoApiOperationFactory operationFactory) : IGoApiClient
{
    private readonly GoApiOperationFactory operationFactory = operationFactory ?? throw new ArgumentNullException(nameof(operationFactory));

    public async Task<GoApiMetricsResponse?> GetMetricsByKeyAsync(string serverKey, DateTime start, DateTime endTime, string? granularity = null)
    {
        var operation = operationFactory.CreateGetMetricsByKey(serverKey, start, endTime, granularity);
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiMetricsResponse?> GetMetricsAsync(string serverId, DateTime start, DateTime endTime, string? granularity = null)
    {
        var operation = operationFactory.CreateGetMetrics(serverId, start, endTime, granularity);
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiMetricsResponse?> GetRealtimeMetricsAsync(string serverId, TimeSpan? duration = null)
    {
        var operation = operationFactory.CreateGetRealtimeMetrics(serverId, duration);
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiServerInfo?> ValidateServerKeyAsync(string serverKey)
    {
        var operation = operationFactory.CreateValidateServerKey(serverKey);
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiStaticInfo?> GetStaticInfoAsync(string serverKey)
    {
        var operation = operationFactory.CreateGetStaticInfo(serverKey);
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiServerInfo?> GetServerInfoAsync(string serverId)
    {
        var operation = operationFactory.CreateGetServerInfo(serverId);
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiMetricsResponse?> GetDashboardMetricsAsync(string serverId)
    {
        var operation = operationFactory.CreateGetDashboardMetrics(serverId);
        return await operation.ExecuteAsync();
    }

    public async Task<List<GoApiServerInfo>?> GetServersListAsync()
    {
        var operation = operationFactory.CreateGetServersList();
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiSourceResponse?> AddServerSourceAsync(string serverId, string source)
    {
        var operation = operationFactory.CreateAddServerSource(serverId, source);
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiSourceResponse?> AddServerSourceByKeyAsync(string serverKey, string source)
    {
        var operation = operationFactory.CreateAddServerSourceByKey(serverKey, source);
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersAsync(string serverId, GoApiSourceIdentifiersRequest request)
    {
        var operation = operationFactory.CreateAddSourceIdentifiers(serverId, request);
        return await operation.ExecuteAsync();
    }

    public async Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersByKeyAsync(string serverKey, GoApiSourceIdentifiersRequest request)
    {
        var operation = operationFactory.CreateAddSourceIdentifiersByKey(serverKey, request);
        return await operation.ExecuteAsync();
    }

    public async Task<List<GoApiServerInfo>?> FindServersByTelegramIdAsync(long telegramId)
    {
        var operation = operationFactory.CreateFindServersByTelegramId(telegramId);
        return await operation.ExecuteAsync();
    }
}
