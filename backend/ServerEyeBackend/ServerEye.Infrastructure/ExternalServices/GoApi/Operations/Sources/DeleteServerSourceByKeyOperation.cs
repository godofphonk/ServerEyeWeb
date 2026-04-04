namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Sources;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to delete server source by server key.
/// </summary>
public class DeleteServerSourceByKeyOperation(
    GoApiHttpHandler httpHandler,
    GoApiLogger logger,
    string serverKey,
    string source) : GoApiOperation<GoApiDeleteSourceResponse?>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildDeleteServerSourceByKeyUrl(serverKey, source);
    }

    protected override async Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        Logger.LogDebug(GetOperationName(), "Deleting source", new { ServerKey = serverKey, Source = source });
        return await HttpHandler.DeleteAsync(url);
    }

    protected override GoApiDeleteSourceResponse? ProcessResponse(string content)
    {
        return GoApiJsonSerializer.DeserializeDeleteSourceResponse(content);
    }

    protected override string GetOperationName()
    {
        return "DeleteServerSourceByKey";
    }

    protected override object CreateLogData(GoApiDeleteSourceResponse? result)
    {
        return new
        {
            ServerKey = serverKey,
            Source = source,
            DeletedIdentifiers = result?.DeletedIdentifiers.Count ?? 0
        };
    }
}
