namespace ServerEye.Infrastracture.ExternalServices.GoApi;

/// <summary>
/// URL building and parameter encoding for Go API endpoints.
/// </summary>
public class GoApiUrlBuilder
{
    /// <summary>
    /// Builds metrics URL with parameters.
    /// </summary>
    public string BuildMetricsUrl(string serverId, DateTime start, DateTime end, string? granularity = null)
    {
        var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endStr = end.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"/api/servers/{serverId}/metrics/tiered?start={startStr}&end={endStr}";

        if (!string.IsNullOrEmpty(granularity))
        {
            url += $"&granularity={granularity}";
        }

        return url;
    }

    /// <summary>
    /// Builds metrics URL by server key with parameters.
    /// </summary>
    public string BuildMetricsByKeyUrl(string serverKey, DateTime start, DateTime end, string? granularity = null)
    {
        var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endStr = end.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/metrics?start={startStr}&end={endStr}";

        if (!string.IsNullOrEmpty(granularity))
        {
            url += $"&granularity={granularity}";
        }

        return url;
    }

    /// <summary>
    /// Builds server validation URL by key.
    /// </summary>
    public string BuildServerValidationUrl(string serverKey)
    {
        return $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/metrics";
    }

    /// <summary>
    /// Builds static info URL by server key.
    /// </summary>
    public string BuildStaticInfoUrl(string serverKey)
    {
        return $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/static-info";
    }

    /// <summary>
    /// Builds server info URL.
    /// </summary>
    public string BuildServerInfoUrl(string serverId)
    {
        return $"/api/servers/{serverId}";
    }

    /// <summary>
    /// Builds servers list URL.
    /// </summary>
    public string BuildServersListUrl()
    {
        return "/api/servers";
    }

    /// <summary>
    /// Builds add server source URL.
    /// </summary>
    public string BuildAddServerSourceUrl(string serverId)
    {
        return $"/api/servers/{serverId}/sources";
    }

    /// <summary>
    /// Builds add server source URL by key.
    /// </summary>
    public string BuildAddServerSourceByKeyUrl(string serverKey)
    {
        return $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/sources";
    }

    /// <summary>
    /// Builds add server source identifiers URL.
    /// </summary>
    public string BuildAddServerSourceIdentifiersUrl(string serverId)
    {
        return $"/api/servers/{serverId}/sources/identifiers";
    }

    /// <summary>
    /// Builds add server source identifiers URL by key.
    /// </summary>
    public string BuildAddServerSourceIdentifiersByKeyUrl(string serverKey)
    {
        return $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/sources/identifiers";
    }

    /// <summary>
    /// Builds find servers by telegram ID URL.
    /// </summary>
    public string BuildFindServersByTelegramIdUrl(long telegramId)
    {
        return $"/api/servers/by-telegram/{telegramId}";
    }
}
