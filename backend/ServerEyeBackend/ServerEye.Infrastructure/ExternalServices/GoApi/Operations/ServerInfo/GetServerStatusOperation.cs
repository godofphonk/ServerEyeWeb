namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.ServerInfo;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to get server status by server key.
/// </summary>
public class GetServerStatusOperation(
    GoApiHttpHandler httpHandler, 
    GoApiLogger logger,
    string serverKey) : GoApiOperation<GoApiServerStatus>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildServerStatusUrl(serverKey);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override GoApiServerStatus? ProcessResponse(string content)
    {
        return GoApiJsonSerializer.DeserializeServerStatus(content);
    }

    protected override string GetOperationName()
    {
        return "GetServerStatus";
    }

    protected override object CreateLogData(GoApiServerStatus? result)
    {
        return new { result?.Hostname, result?.AgentVersion, result?.Online };
    }
}
