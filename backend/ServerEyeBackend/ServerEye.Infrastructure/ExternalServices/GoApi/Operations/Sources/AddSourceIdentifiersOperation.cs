namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Sources;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to add server source identifiers by server ID.
/// </summary>
public class AddSourceIdentifiersOperation(
    GoApiHttpHandler httpHandler, 
    GoApiLogger logger,
    string serverId,
    GoApiSourceIdentifiersRequest request) : GoApiOperation<GoApiSourceIdentifiersResponse?>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildAddServerSourceIdentifiersUrl(serverId);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.PostAsJsonAsync(url, request);
    }

    protected override GoApiSourceIdentifiersResponse? ProcessResponse(string content)
    {
        return GoApiJsonSerializer.DeserializeSourceIdentifiersResponse(content);
    }

    protected override string GetOperationName()
    {
        return "AddServerSourceIdentifiers";
    }

    protected override object CreateLogData(GoApiSourceIdentifiersResponse? result)
    {
        return new { request.SourceType };
    }
}
