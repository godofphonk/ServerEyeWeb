namespace ServerEye.Infrastracture.ExternalServices.GoApi.Operations.ServerInfo;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastracture.ExternalServices.GoApi;
using ServerEye.Infrastracture.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to get server information by server ID.
/// </summary>
public class GetServerInfoOperation(
    GoApiHttpHandler httpHandler, 
    GoApiLogger logger,
    string serverId) : GoApiOperation<GoApiServerInfo?>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildServerInfoUrl(serverId);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override GoApiServerInfo? ProcessResponse(string content)
    {
        return GoApiJsonSerializer.DeserializeServerInfo(content);
    }

    protected override string GetOperationName()
    {
        return "GetServerInfo";
    }

    protected override object CreateLogData(GoApiServerInfo? result)
    {
        return new { result?.ServerId };
    }
}
