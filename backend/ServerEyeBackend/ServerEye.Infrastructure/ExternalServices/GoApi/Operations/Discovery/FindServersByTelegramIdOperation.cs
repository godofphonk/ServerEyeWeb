namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Discovery;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;
using System.Text.Json;

/// <summary>
/// Operation to find servers by Telegram ID.
/// </summary>
public class FindServersByTelegramIdOperation : GoApiOperation<List<GoApiServerInfo>?>
{
    private readonly long telegramId;

    public FindServersByTelegramIdOperation(
        GoApiHttpHandler httpHandler, 
        GoApiLogger logger,
        long telegramId)
        : base(httpHandler, logger) =>
        this.telegramId = telegramId;
    protected override Uri BuildUrl()
    {
        return GoApiUrlBuilder.BuildFindServersByTelegramIdUrl(telegramId);
    }

    protected override Task<HttpResponseMessage> ExecuteRequestAsync(Uri url)
    {
        return HttpHandler.GetAsync(url);
    }

    protected override List<GoApiServerInfo>? ProcessResponse(string content)
    {
        // Parse the Go API response structure
        using var jsonDoc = JsonDocument.Parse(content ?? string.Empty);
        var serversElement = jsonDoc.RootElement.GetProperty("servers");
        
        var servers = JsonSerializer.Deserialize<List<GoApiServerInfo>>(serversElement.GetRawText());
        var count = servers?.Count ?? 0;
        
        return servers ?? new List<GoApiServerInfo>();
    }

    protected override string GetOperationName()
    {
        return "FindServersByTelegramId";
    }

    protected override async Task<List<GoApiServerInfo>?> HandleErrorAsync(HttpResponseMessage response, GoApiPerformanceTracker perfTracker, long requestTime)
    {
        // For 404 errors, return empty list (no servers found)
        if (GoApiHttpHandler.IsNotFound(response))
        {
            Logger.LogResponse(GetOperationName(), BuildUrl(), perfTracker.ElapsedMilliseconds, new { TelegramId = telegramId, Found = 0 });
            return new List<GoApiServerInfo>();
        }

        // For other errors, use default error handling
        return await base.HandleErrorAsync(response, perfTracker, requestTime);
    }

    protected override object CreateLogData(List<GoApiServerInfo>? result)
    {
        return new
        {
            TelegramId = telegramId,
            Count = result?.Count ?? 0
        };
    }
}
