namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Metrics;

using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to get metrics by server ID.
/// </summary>
public class GetMetricsOperation(
    GoApiHttpHandler httpHandler,
    GoApiLogger logger,
    string serverId,
    DateTime startTime,
    DateTime endTime,
    string? granularity = null) : MetricsOperation(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildMetricsUrl(serverId, startTime, endTime, granularity);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override string GetOperationName()
    {
        return "GetMetrics";
    }

    protected override DateTime GetStartTime()
    {
        return startTime;
    }

    protected override DateTime GetEndTime()
    {
        return endTime;
    }

    protected override string? GetGranularity()
    {
        return granularity;
    }

    protected override string GetServerIdentifier()
    {
        return serverId;
    }
}
