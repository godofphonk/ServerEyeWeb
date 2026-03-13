namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Sources;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to get server source identifiers by server key.
/// </summary>
public class GetSourceIdentifiersByKeyOperation(
    GoApiHttpHandler httpHandler, 
    GoApiLogger logger,
    string serverKey) : GoApiOperation<GoApiSourceIdentifiersResponse?>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildDeleteServerSourceIdentifiersByKeyUrl(serverKey);
    }

    protected override async Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return await HttpHandler.GetAsync(url);
    }

    protected override GoApiSourceIdentifiersResponse? ProcessResponse(string content)
    {
        return GoApiJsonSerializer.DeserializeSourceIdentifiersResponse(content);
    }

    protected override string GetOperationName()
    {
        return "GetSourceIdentifiersByKey";
    }

    protected override object CreateLogData(GoApiSourceIdentifiersResponse? result)
    {
        return new
        {
            ServerKey = serverKey,
            Sources = result?.Sources ?? new List<string>(),
            IdentifierCount = result?.Identifiers?.Sum(kvp => kvp.Value?.Count ?? 0) ?? 0
        };
    }
}
