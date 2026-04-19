namespace ServerEye.Infrastructure.ExternalServices.GoApi;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Discovery;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Metrics;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.ServerInfo;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Sources;

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
    public GetUnifiedMetricsOperation CreateGetUnifiedMetrics(string serverKey, bool includeMetrics = true, bool includeStatus = true, bool includeStatic = true)
    {
        return new GetUnifiedMetricsOperation(httpHandler, logger, serverKey, includeMetrics, includeStatus, includeStatic);
    }

    public GetTieredMetricsByKeyOperation CreateGetTieredMetricsByKey(string serverKey, DateTime startTime, DateTime endTime, string? granularity = null)
    {
        return new GetTieredMetricsByKeyOperation(httpHandler, logger, serverKey, startTime, endTime, granularity);
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

    public GetServerStatusOperation CreateGetServerStatus(string serverKey)
    {
        return new GetServerStatusOperation(httpHandler, logger, serverKey);
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

    public GetSourceIdentifiersByKeyOperation CreateGetSourceIdentifiersByKey(string serverKey)
    {
        return new GetSourceIdentifiersByKeyOperation(httpHandler, logger, serverKey);
    }

    // Source Deletion Operations
    public DeleteServerSourceByKeyOperation CreateDeleteServerSourceByKey(string serverKey, string source)
    {
        return new DeleteServerSourceByKeyOperation(httpHandler, logger, serverKey, source);
    }

    public DeleteSourceIdentifiersByKeyOperation CreateDeleteSourceIdentifiersByKey(string serverKey, GoApiDeleteSourceIdentifiersRequest request)
    {
        return new DeleteSourceIdentifiersByKeyOperation(httpHandler, logger, serverKey, request);
    }

    public DeleteSourceIdentifiersByTypeOperation CreateDeleteSourceIdentifiersByType(string serverKey, string sourceType, GoApiDeleteSourceIdentifiersRequest request)
    {
        return new DeleteSourceIdentifiersByTypeOperation(httpHandler, logger, serverKey, sourceType, request);
    }

    // Discovery Operations
    public FindServersByTelegramIdOperation CreateFindServersByTelegramId(long telegramId)
    {
        return new FindServersByTelegramIdOperation(httpHandler, logger, telegramId);
    }
}
