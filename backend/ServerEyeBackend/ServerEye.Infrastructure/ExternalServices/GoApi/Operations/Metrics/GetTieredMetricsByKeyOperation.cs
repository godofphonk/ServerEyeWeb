namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Metrics;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

public class GetTieredMetricsByKeyOperation : MetricsOperation
{
    private readonly string serverKey;
    private readonly DateTime startTime;
    private readonly DateTime endTime;
    private readonly string? granularity;

    public GetTieredMetricsByKeyOperation(
        GoApiHttpHandler httpHandler,
        GoApiLogger logger,
        string serverKey,
        DateTime startTime,
        DateTime endTime,
        string? granularity = null)
        : base(httpHandler, logger)
    {
        this.serverKey = serverKey ?? throw new ArgumentNullException(nameof(serverKey));
        this.startTime = startTime;
        this.endTime = endTime;
        this.granularity = granularity;
    }

    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildTieredMetricsByKeyUrl(this.serverKey, this.startTime, this.endTime, this.granularity);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override string GetOperationName() => "GetTieredMetricsByKey";

    protected override DateTime GetStartTime() => this.startTime;

    protected override DateTime GetEndTime() => this.endTime;

    protected override string? GetGranularity() => this.granularity;

    protected override string GetServerIdentifier() => this.serverKey;
}
