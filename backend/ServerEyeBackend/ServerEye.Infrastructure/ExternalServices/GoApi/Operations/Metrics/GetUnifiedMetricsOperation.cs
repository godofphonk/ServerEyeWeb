namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Metrics;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to get unified server data (metrics, status, static info).
/// </summary>
public class GetUnifiedMetricsOperation : GoApiOperation<GoApiUnifiedResponse?>
{
    private readonly string serverKey;
    private readonly bool includeMetrics;
    private readonly bool includeStatus;
    private readonly bool includeStatic;

    public GetUnifiedMetricsOperation(
        GoApiHttpHandler httpHandler,
        GoApiLogger logger,
        string serverKey,
        bool includeMetrics = true,
        bool includeStatus = true,
        bool includeStatic = true)
        : base(httpHandler, logger)
    {
        this.serverKey = serverKey ?? throw new ArgumentNullException(nameof(serverKey));
        this.includeMetrics = includeMetrics;
        this.includeStatus = includeStatus;
        this.includeStatic = includeStatic;
    }

    protected override Uri BuildUrl()
    {
        // Call Go API unified endpoint directly - it returns metrics, status, and static info
        return GoApiUrlBuilder.BuildUnifiedUrl(this.serverKey, this.includeMetrics, this.includeStatus, this.includeStatic);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override GoApiUnifiedResponse? ProcessResponse(string content)
    {
        // Log raw response for debugging
        Logger.LogDebug(GetOperationName(), "Raw response", new { Content = content[..Math.Min(1000, content.Length)] });

        var response = GoApiJsonSerializer.DeserializeUnifiedResponse(content);

        // Transform flat snake_case fields to nested camelCase objects for frontend compatibility
        if (response?.Metrics?.DataPoints != null)
        {
            foreach (var dataPoint in response.Metrics.DataPoints)
            {
                // If nested objects are null, populate them from flat fields
                dataPoint.Network ??= new NetworkMetrics { Avg = dataPoint.NetworkAvg, Max = dataPoint.NetworkMax };
                dataPoint.LoadAverage ??= new LoadAverageMetrics { Avg = dataPoint.LoadAvg, Max = dataPoint.LoadMax };
                dataPoint.Temperature ??= new TemperatureMetrics { Avg = dataPoint.TempAvg, Max = dataPoint.TempMax };
            }
        }

        return response;
    }

    protected override string GetOperationName()
    {
        return "GetUnifiedMetrics";
    }
}
