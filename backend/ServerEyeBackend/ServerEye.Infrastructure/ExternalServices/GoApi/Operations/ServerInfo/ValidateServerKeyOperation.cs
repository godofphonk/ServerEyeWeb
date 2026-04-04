namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.ServerInfo;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to validate server key and return server information.
/// </summary>
public class ValidateServerKeyOperation : GoApiOperation<GoApiServerInfo?>
{
    private readonly string serverKey;

    public ValidateServerKeyOperation(
        GoApiHttpHandler httpHandler,
        GoApiLogger logger,
        string serverKey)
        : base(httpHandler, logger) =>
        this.serverKey = serverKey ?? throw new ArgumentNullException(nameof(serverKey));
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildServerValidationUrl(serverKey);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override GoApiServerInfo? ProcessResponse(string content)
    {
        var metricsResponse = GoApiJsonSerializer.DeserializeMetricsResponse(content);

        if (metricsResponse?.ServerId != null)
        {
            return new GoApiServerInfo
            {
                ServerId = metricsResponse.ServerId,
                ServerKey = serverKey,
                Hostname = metricsResponse.Status?.Hostname ?? "Unknown",
                OperatingSystem = metricsResponse.Status?.OperatingSystem ?? "Unknown",
                AgentVersion = metricsResponse.Status?.AgentVersion ?? "Unknown",
                LastSeen = metricsResponse.Status?.LastSeen ?? DateTime.UtcNow
            };
        }

        return null;
    }

    protected override string GetOperationName()
    {
        return "ValidateServerKey";
    }

    protected override async Task<GoApiServerInfo?> HandleErrorAsync(HttpResponseMessage response, GoApiPerformanceTracker perfTracker, long requestTime)
    {
        var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
        Logger.LogError(GetOperationName(), BuildUrl(), requestTime, (int)response.StatusCode, errorContent);

        // For 404 errors, return null (server not found)
        if (GoApiHttpHandler.IsNotFound(response))
        {
            return null;
        }

        // For other errors, throw appropriate exception
        throw GoApiErrorHandler.MapException(new InvalidOperationException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"));
    }
}
