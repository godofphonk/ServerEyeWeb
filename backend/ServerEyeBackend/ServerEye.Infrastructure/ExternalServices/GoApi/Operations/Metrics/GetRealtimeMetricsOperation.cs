namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Metrics;

using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to get realtime metrics for a server.
/// </summary>
public class GetRealtimeMetricsOperation(
    GoApiHttpHandler httpHandler,
    GoApiLogger logger,
    string serverId,
    TimeSpan? duration = null) : MetricsOperation(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.Subtract(duration ?? TimeSpan.FromMinutes(5));
        return GoApiUrlBuilder.BuildMetricsUrl(serverId, startTime, endTime);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override string GetOperationName()
    {
        return "GetRealtimeMetrics";
    }

    protected override DateTime GetStartTime()
    {
        return DateTime.UtcNow.Subtract(duration ?? TimeSpan.FromMinutes(5));
    }

    protected override DateTime GetEndTime()
    {
        return DateTime.UtcNow;
    }

    protected override string? GetGranularity()
    {
        return null;
    }

    protected override string GetServerIdentifier()
    {
        return serverId;
    }
}
