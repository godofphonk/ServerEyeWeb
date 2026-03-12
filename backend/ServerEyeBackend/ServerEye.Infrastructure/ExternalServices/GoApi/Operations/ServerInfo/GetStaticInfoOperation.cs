namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.ServerInfo;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

/// <summary>
/// Operation to get static server information by server key.
/// </summary>
public class GetStaticInfoOperation(
    GoApiHttpHandler httpHandler, 
    GoApiLogger logger,
    string serverKey) : GoApiOperation<GoApiStaticInfo?>(httpHandler, logger)
{
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildStaticInfoUrl(serverKey);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override GoApiStaticInfo? ProcessResponse(string content)
    {
        var goApiResponse = GoApiJsonSerializer.DeserializeStaticInfoResponse(content);
        if (goApiResponse == null)
        {
            return null;
        }

        return GoApiDataTransformer.ConvertToStaticInfo(goApiResponse);
    }

    protected override string GetOperationName()
    {
        return "GetStaticInfo";
    }

    protected override object CreateLogData(GoApiStaticInfo? result)
    {
        return new { result?.ServerId };
    }
}
