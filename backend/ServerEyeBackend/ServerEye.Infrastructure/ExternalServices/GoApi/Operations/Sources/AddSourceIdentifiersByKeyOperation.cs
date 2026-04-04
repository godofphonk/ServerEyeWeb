namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Sources;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to add server source identifiers by server key.
/// </summary>
public class AddSourceIdentifiersByKeyOperation(
    GoApiHttpHandler httpHandler,
    GoApiLogger logger,
    string serverKey,
    GoApiSourceIdentifiersRequest request) : GoApiOperation<GoApiSourceIdentifiersResponse?>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildAddServerSourceIdentifiersByKeyUrl(serverKey);
    }

    protected override async Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        // Log the exact JSON being sent for debugging
        var jsonRequest = GoApiJsonSerializer.SerializeForDebug(request);
        Logger.LogDebug(GetOperationName(), "JSON request to Go API", jsonRequest);

        return await HttpHandler.PostAsJsonAsync(url, request);
    }

    protected override GoApiSourceIdentifiersResponse? ProcessResponse(string content)
    {
        return GoApiJsonSerializer.DeserializeSourceIdentifiersResponse(content);
    }

    protected override string GetOperationName()
    {
        return "AddServerSourceIdentifiersByKey";
    }

    protected override object CreateLogData(GoApiSourceIdentifiersResponse? result)
    {
        return new
        {
            request.SourceType,
            request.TelegramId
        };
    }
}
