namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Sources;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to delete server source identifiers by source type.
/// </summary>
public class DeleteSourceIdentifiersByTypeOperation(
    GoApiHttpHandler httpHandler,
    GoApiLogger logger,
    string serverKey,
    string sourceType,
    GoApiDeleteSourceIdentifiersRequest request) : GoApiOperation<GoApiDeleteSourceResponse?>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildDeleteServerSourceIdentifiersByTypeUrl(serverKey, sourceType);
    }

    protected override async Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        // Log the exact JSON being sent for debugging
        var jsonRequest = GoApiJsonSerializer.SerializeForDebug(request);
        Logger.LogDebug(GetOperationName(), "JSON request to Go API", jsonRequest);

        return await HttpHandler.DeleteAsJsonAsync(url, request);
    }

    protected override GoApiDeleteSourceResponse? ProcessResponse(string content)
    {
        return GoApiJsonSerializer.DeserializeDeleteSourceResponse(content);
    }

    protected override string GetOperationName()
    {
        return "DeleteServerSourceIdentifiersByType";
    }

    protected override object CreateLogData(GoApiDeleteSourceResponse? result)
    {
        return new
        {
            ServerKey = serverKey,
            SourceType = sourceType,
            IdentifierCount = request.Identifiers.Count,
            DeletedIdentifiers = result?.DeletedIdentifiers.Count ?? 0
        };
    }
}
