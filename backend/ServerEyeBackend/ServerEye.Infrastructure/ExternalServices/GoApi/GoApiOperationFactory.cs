namespace ServerEye.Infrastructure.ExternalServices.GoApi;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Metrics;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.ServerInfo;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Sources;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Discovery;

/// <summary>
/// Factory for creating Go API operations.
/// Implements Factory Pattern to centralize operation creation and dependency injection.
/// </summary>
public class GoApiOperationFactory
{
    private readonly GoApiHttpHandler httpHandler;
    private readonly GoApiLogger logger;

    public GoApiOperationFactory(GoApiHttpHandler httpHandler, GoApiLogger logger)
    {
        this.httpHandler = httpHandler ?? throw new ArgumentNullException(nameof(httpHandler));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Metrics Operations
    public GetMetricsOperation CreateGetMetrics(string serverId, DateTime startTime, DateTime endTime, string? granularity = null)
    {
        return new GetMetricsOperation(httpHandler, logger, serverId, startTime, endTime, granularity);
    }

    public GetMetricsByKeyOperation CreateGetMetricsByKey(string serverKey, DateTime startTime, DateTime endTime, string? granularity = null)
    {
        return new GetMetricsByKeyOperation(httpHandler, logger, serverKey, startTime, endTime, granularity);
    }

    public GetRealtimeMetricsOperation CreateGetRealtimeMetrics(string serverId, TimeSpan? duration = null)
    {
        return new GetRealtimeMetricsOperation(httpHandler, logger, serverId, duration);
    }

    public GetDashboardMetricsOperation CreateGetDashboardMetrics(string serverId)
    {
        return new GetDashboardMetricsOperation(httpHandler, logger, serverId);
    }

    // Server Info Operations
    public GetServerInfoOperation CreateGetServerInfo(string serverId)
    {
        return new GetServerInfoOperation(httpHandler, logger, serverId);
    }

    public GetStaticInfoOperation CreateGetStaticInfo(string serverKey)
    {
        return new GetStaticInfoOperation(httpHandler, logger, serverKey);
    }

    public ValidateServerKeyOperation CreateValidateServerKey(string serverKey)
    {
        return new ValidateServerKeyOperation(httpHandler, logger, serverKey);
    }

    public GetServersListOperation CreateGetServersList()
    {
        return new GetServersListOperation(httpHandler, logger);
    }

    // Source Management Operations
    public AddServerSourceOperation CreateAddServerSource(string serverId, string source)
    {
        return new AddServerSourceOperation(httpHandler, logger, serverId, source);
    }

    public AddServerSourceByKeyOperation CreateAddServerSourceByKey(string serverKey, string source)
    {
        return new AddServerSourceByKeyOperation(httpHandler, logger, serverKey, source);
    }

    public AddSourceIdentifiersOperation CreateAddSourceIdentifiers(string serverId, GoApiSourceIdentifiersRequest request)
    {
        return new AddSourceIdentifiersOperation(httpHandler, logger, serverId, request);
    }

    public AddSourceIdentifiersByKeyOperation CreateAddSourceIdentifiersByKey(string serverKey, GoApiSourceIdentifiersRequest request)
    {
        return new AddSourceIdentifiersByKeyOperation(httpHandler, logger, serverKey, request);
    }

    // Discovery Operations
    public FindServersByTelegramIdOperation CreateFindServersByTelegramId(long telegramId)
    {
        return new FindServersByTelegramIdOperation(httpHandler, logger, telegramId);
    }
}
