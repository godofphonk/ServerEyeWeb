namespace ServerEye.Infrastracture.ExternalServices;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.Interfaces.Services;

public class GoApiClient : IGoApiClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger<GoApiClient> logger;

    public GoApiClient(HttpClient httpClient, ILogger<GoApiClient> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<GoApiMetricsResponse?> GetMetricsAsync(string serverId, DateTime start, DateTime endTime, string? granularity = null)
    {
        try
        {
            var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endStr = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var url = $"/api/servers/{serverId}/metrics/tiered?start={startStr}&end={endStr}";

            if (!string.IsNullOrEmpty(granularity))
            {
                url += $"&granularity={granularity}";
            }

            this.logger.LogInformation("Requesting metrics from Go API: {Url}", url);

            var response = await this.httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this.logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<GoApiMetricsResponse>();
            this.logger.LogInformation("Successfully retrieved {Points} data points", result?.TotalPoints ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error calling Go API for metrics");
            return null;
        }
    }

    public async Task<GoApiMetricsResponse?> GetRealtimeMetricsAsync(string serverId, TimeSpan? duration = null)
    {
        try
        {
            var url = $"/api/servers/{serverId}/metrics/realtime";

            if (duration.HasValue)
            {
                var durationStr = $"{(int)duration.Value.TotalMinutes}m";
                url += $"?duration={durationStr}";
            }

            this.logger.LogInformation("Requesting realtime metrics from Go API: {Url}", url);

            var response = await this.httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this.logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoApiMetricsResponse>();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error calling Go API for realtime metrics");
            return null;
        }
    }

    public async Task<GoApiMetricsResponse?> GetDashboardMetricsAsync(string serverId)
    {
        try
        {
            var url = $"/api/servers/{serverId}/metrics/dashboard";

            this.logger.LogInformation("Requesting dashboard metrics from Go API: {Url}", url);

            var response = await this.httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this.logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoApiMetricsResponse>();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error calling Go API for dashboard metrics");
            return null;
        }
    }

    public async Task<GoApiServerInfo?> ValidateServerKeyAsync(string serverKey)
    {
        try
        {
            var serverId = serverKey.Replace("key_", "srv_", StringComparison.Ordinal);
            var now = DateTime.UtcNow;
            var start = now.AddMinutes(-5);
            var url = $"/api/servers/{Uri.EscapeDataString(serverId)}/metrics/realtime?duration=5m";

            this.logger.LogInformation("Validating server key by checking metrics (public endpoint): {Url}", url);

            var response = await this.httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this.logger.LogWarning("Server key validation failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            var metricsResponse = await response.Content.ReadFromJsonAsync<GoApiMetricsResponse>();
            
            if (metricsResponse?.ServerId != null)
            {
                return new GoApiServerInfo
                {
                    ServerId = metricsResponse.ServerId,
                    ServerKey = serverKey,
                    Hostname = "Unknown",
                    OperatingSystem = "Unknown",
                    AgentVersion = "Unknown",
                    LastSeen = DateTime.UtcNow
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error validating server key with Go API");
            return null;
        }
    }

    public async Task<GoApiServerInfo?> GetServerInfoAsync(string serverId)
    {
        try
        {
            var url = $"/api/servers/{serverId}";

            this.logger.LogInformation("Requesting server info from Go API: {Url}", url);

            var response = await this.httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this.logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoApiServerInfo>();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error calling Go API for server info");
            return null;
        }
    }

    public async Task<List<GoApiServerInfo>?> GetServersListAsync()
    {
        try
        {
            var url = "/api/servers";

            this.logger.LogInformation("Requesting servers list from Go API");

            var response = await this.httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this.logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<List<GoApiServerInfo>>();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error calling Go API for servers list");
            return null;
        }
    }
}
