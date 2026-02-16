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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endStr = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var url = $"/api/servers/{serverId}/metrics/tiered?start={startStr}&end={endStr}";

            if (!string.IsNullOrEmpty(granularity))
            {
                url += $"&granularity={granularity}";
            }

            this.logger.LogInformation("[PERF] Requesting metrics from Go API: {Url}", url);

            var response = await this.httpClient.GetAsync(new Uri(url, UriKind.Relative));
            var requestTime = stopwatch.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this.logger.LogError("[PERF] Go API error after {Ms}ms: {StatusCode} - {Content}", requestTime, response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<GoApiMetricsResponse>();
            stopwatch.Stop();
            var totalTime = stopwatch.ElapsedMilliseconds;

            if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
            {
                this.logger.LogWarning(
                    "[PERF] Go API returned empty data after {Ms}ms for server {ServerId}",
                    totalTime,
                    serverId);
                return result;
            }

            this.logger.LogInformation(
                "[PERF] Successfully retrieved {Points} data points in {Ms}ms (request: {RequestMs}ms, parse: {ParseMs}ms)",
                result.TotalPoints,
                totalTime,
                requestTime,
                totalTime - requestTime);

            return result;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            this.logger.LogError("[PERF] Go API request timeout after {Ms}ms: {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.logger.LogError(ex, "[PERF] Error calling Go API for metrics after {Ms}ms", stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    public async Task<GoApiMetricsResponse?> GetRealtimeMetricsAsync(string serverId, TimeSpan? duration = null)
    {
        try
        {
            var actualDuration = duration ?? TimeSpan.FromMinutes(5);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(actualDuration);

            this.logger.LogInformation("Requesting realtime metrics for server {ServerId} from {Start} to {End}", serverId, startTime, endTime);

            return await this.GetMetricsAsync(serverId, startTime, endTime);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error calling Go API for realtime metrics");
            return null;
        }
    }

    public async Task<GoApiServerInfo?> ValidateServerKeyAsync(string serverKey)
    {
        try
        {
            var url = $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/metrics";

            this.logger.LogInformation("Validating server key with Go API: {Url}", url);

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
                    Hostname = metricsResponse.Status?.Hostname ?? "Unknown",
                    OperatingSystem = metricsResponse.Status?.OperatingSystem ?? "Unknown",
                    AgentVersion = metricsResponse.Status?.AgentVersion ?? "Unknown",
                    LastSeen = metricsResponse.Status?.LastSeen ?? DateTime.UtcNow
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

    public async Task<GoApiMetricsResponse?> GetDashboardMetricsAsync(string serverId)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-5);

            this.logger.LogInformation("Getting dashboard metrics for server {ServerId} (last 5 minutes)", serverId);
            return await this.GetMetricsAsync(serverId, startTime, endTime);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error calling Go API for dashboard metrics");
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
