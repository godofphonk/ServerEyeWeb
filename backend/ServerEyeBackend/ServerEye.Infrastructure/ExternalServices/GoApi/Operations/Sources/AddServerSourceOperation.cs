namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Sources;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to add a server source by server ID.
/// </summary>
public class AddServerSourceOperation(
    GoApiHttpHandler httpHandler,
    GoApiLogger logger,
    string serverId,
    string source) : GoApiOperation<GoApiSourceResponse?>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildAddServerSourceUrl(serverId);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        var request = new GoApiSourceRequest { Source = source };
        return HttpHandler.PostAsJsonAsync(url, request);
    }

    protected override GoApiSourceResponse? ProcessResponse(string content)
    {
        return GoApiJsonSerializer.DeserializeSourceResponse(content);
    }

    protected override string GetOperationName()
    {
        return "AddServerSource";
    }

    protected override object CreateLogData(GoApiSourceResponse? result)
    {
        return new { Source = source };
    }
}
