namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.ServerInfo;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to get list of all servers.
/// </summary>
public class GetServersListOperation(
    GoApiHttpHandler httpHandler, 
    GoApiLogger logger) : GoApiOperation<List<GoApiServerInfo>?>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildServersListUrl();
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override List<GoApiServerInfo>? ProcessResponse(string content)
    {
        return GoApiJsonSerializer.DeserializeServersList(content);
    }

    protected override string GetOperationName()
    {
        return "GetServersList";
    }

    protected override object CreateLogData(List<GoApiServerInfo>? result)
    {
        return new { Count = result?.Count ?? 0 };
    }
}
