namespace ServerEye.Infrastructure.ExternalServices.GoApi;

/// <summary>
/// URL building and parameter encoding for Go API endpoints.
/// </summary>
public static class GoApiUrlBuilder
{
    /// <summary>
    /// Builds metrics URL with parameters.
    /// </summary>
    public static Uri BuildMetricsUrl(string serverId, DateTime start, DateTime end, string? granularity = null)
    {
        var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endStr = end.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"/api/servers/{serverId}/metrics/tiered?start={startStr}&end={endStr}";

        if (!string.IsNullOrEmpty(granularity))
        {
            url += $"&granularity={granularity}";
        }

        return new Uri(url, UriKind.Relative);
    }

    /// <summary>
    /// Builds metrics URL by server key with parameters.
    /// </summary>
    public static Uri BuildMetricsByKeyUrl(string serverKey, DateTime start, DateTime end, string? granularity = null)
    {
        var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endStr = end.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/metrics?start={startStr}&end={endStr}";

        if (!string.IsNullOrEmpty(granularity))
        {
            url += $"&granularity={granularity}";
        }

        return new Uri(url, UriKind.Relative);
    }

    /// <summary>
    /// Builds server validation URL by key.
    /// </summary>
    public static Uri BuildServerValidationUrl(string serverKey)
    {
        return new Uri($"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/metrics", UriKind.Relative);
    }

    /// <summary>
    /// Builds static info URL by server key.
    /// </summary>
    public static Uri BuildStaticInfoUrl(string serverKey)
    {
        return new Uri($"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/static-info", UriKind.Relative);
    }

    /// <summary>
    /// Builds server info URL.
    /// </summary>
    public static Uri BuildServerInfoUrl(string serverId)
    {
        return new Uri($"/api/servers/{serverId}", UriKind.Relative);
    }

    /// <summary>
    /// Builds servers list URL.
    /// </summary>
    public static Uri BuildServersListUrl()
    {
        return new Uri("/api/servers", UriKind.Relative);
    }

    /// <summary>
    /// Builds add server source URL.
    /// </summary>
    public static Uri BuildAddServerSourceUrl(string serverId)
    {
        return new Uri($"/api/servers/{serverId}/sources", UriKind.Relative);
    }

    /// <summary>
    /// Builds add server source URL by key.
    /// </summary>
    public static Uri BuildAddServerSourceByKeyUrl(string serverKey)
    {
        return new Uri($"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/sources", UriKind.Relative);
    }

    /// <summary>
    /// Builds add server source identifiers URL.
    /// </summary>
    public static Uri BuildAddServerSourceIdentifiersUrl(string serverId)
    {
        return new Uri($"/api/servers/{serverId}/sources/identifiers", UriKind.Relative);
    }

    /// <summary>
    /// Builds add server source identifiers URL by key.
    /// </summary>
    public static Uri BuildAddServerSourceIdentifiersByKeyUrl(string serverKey)
    {
        return new Uri($"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/sources/identifiers", UriKind.Relative);
    }

    /// <summary>
    /// Builds find servers by telegram ID URL.
    /// </summary>
    public static Uri BuildFindServersByTelegramIdUrl(long telegramId)
    {
        return new Uri($"/api/servers/by-telegram/{telegramId}", UriKind.Relative);
    }
}
