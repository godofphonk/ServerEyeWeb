namespace ServerEye.Infrastracture.ExternalServices;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Infrastracture.ExternalServices.GoApi;
using ServerEye.Core.Exceptions;

/// <summary>
/// Go API client with separated responsibilities.
/// </summary>
public class GoApiClient(
    GoApiHttpHandler httpHandler,
    GoApiLogger logger) : IGoApiClient
{
    private readonly GoApiHttpHandler httpHandler = httpHandler;
    private readonly GoApiLogger logger = logger;

    public async Task<GoApiMetricsResponse?> GetMetricsByKeyAsync(string serverKey, DateTime start, DateTime endTime, string? granularity = null)
    {
        const string operation = "GetMetricsByKey";
        var url = GoApiUrlBuilder.BuildMetricsByKeyUrl(serverKey, start, endTime, granularity);

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);
            var requestTime = perfTracker.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, requestTime, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            // Log raw JSON for debugging network_details structure
            if (content.Contains("network_details", StringComparison.OrdinalIgnoreCase))
            {
                var startIndex = Math.Max(0, content.IndexOf("network_details", StringComparison.OrdinalIgnoreCase) - 100);
                logger.LogDebug(
                    operation,
                    "Raw Go API response contains network_details",
                    content.Substring(startIndex, Math.Min(500, content.Length - startIndex)));
            }

            // Try to parse as time series first
            var result = GoApiJsonSerializer.DeserializeMetricsResponse(content);
            
            // If no data points, try snapshot format
            if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
            {
                var snapshotResponse = GoApiJsonSerializer.DeserializeSnapshotResponse(content);
                if (snapshotResponse != null && snapshotResponse.Metrics != null)
                {
                    result = GoApiDataTransformer.ConvertSnapshotToTimeSeries(snapshotResponse, start, endTime, granularity);
                }
            }

            perfTracker.Stop();
            var totalTime = perfTracker.ElapsedMilliseconds;

            if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
            {
                logger.LogEmptyData(operation, serverKey, totalTime);
                return result;
            }

            logger.LogPerformance(operation, result.TotalPoints, totalTime, requestTime);
            logger.LogResponse(operation, url, totalTime, new { Points = result.TotalPoints });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiMetricsResponse?> GetMetricsAsync(string serverId, DateTime start, DateTime endTime, string? granularity = null)
    {
        const string operation = "GetMetrics";
        var url = GoApiUrlBuilder.BuildMetricsUrl(serverId, start, endTime, granularity);

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);
            var requestTime = perfTracker.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, requestTime, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = GoApiJsonSerializer.DeserializeMetricsResponse(content);
            perfTracker.Stop();
            var totalTime = perfTracker.ElapsedMilliseconds;

            if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
            {
                logger.LogEmptyData(operation, serverId, totalTime);
                return result;
            }

            logger.LogPerformance(operation, result.TotalPoints, totalTime, requestTime);
            logger.LogResponse(operation, url, totalTime, new { Points = result.TotalPoints });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiMetricsResponse?> GetRealtimeMetricsAsync(string serverId, TimeSpan? duration = null)
    {
        try
        {
            var actualDuration = duration ?? TimeSpan.FromMinutes(5);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(actualDuration);

            logger.LogRequest("GetRealtimeMetrics", new Uri($"/servers/{serverId}/realtime", UriKind.Relative));

            return await GetMetricsAsync(serverId, startTime, endTime);
        }
        catch (Exception ex)
        {
            logger.LogException("GetRealtimeMetrics", new Uri($"Server: {serverId}", UriKind.Relative), 0, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiServerInfo?> ValidateServerKeyAsync(string serverKey)
    {
        const string operation = "ValidateServerKey";
        var url = GoApiUrlBuilder.BuildServerValidationUrl(serverKey);

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                
                if (GoApiHttpHandler.IsNotFound(response))
                {
                    return null;
                }
                
                throw GoApiErrorHandler.CreateServerKeyValidationException(response.StatusCode);
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var metricsResponse = GoApiJsonSerializer.DeserializeMetricsResponse(content);

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
        catch (GoApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiStaticInfo?> GetStaticInfoAsync(string serverKey)
    {
        const string operation = "GetStaticInfo";
        var url = GoApiUrlBuilder.BuildStaticInfoUrl(serverKey);

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var goApiResponse = GoApiJsonSerializer.DeserializeStaticInfoResponse(content);
            if (goApiResponse == null)
            {
                return null;
            }

            var result = GoApiDataTransformer.ConvertToStaticInfo(goApiResponse);
            
            perfTracker.Stop();
            logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { result?.ServerId });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiServerInfo?> GetServerInfoAsync(string serverId)
    {
        const string operation = "GetServerInfo";
        var url = GoApiUrlBuilder.BuildServerInfoUrl(serverId);

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = GoApiJsonSerializer.DeserializeServerInfo(content);
            
            perfTracker.Stop();
            logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { result?.ServerId });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiMetricsResponse?> GetDashboardMetricsAsync(string serverId)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-5);

            logger.LogRequest("GetDashboardMetrics", new Uri($"/servers/{serverId}/dashboard", UriKind.Relative));
            
            return await GetMetricsAsync(serverId, startTime, endTime);
        }
        catch (Exception ex)
        {
            logger.LogException("GetDashboardMetrics", new Uri($"Server: {serverId}", UriKind.Relative), 0, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<List<GoApiServerInfo>?> GetServersListAsync()
    {
        const string operation = "GetServersList";
        var url = GoApiUrlBuilder.BuildServersListUrl();

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = GoApiJsonSerializer.DeserializeServersList(content);
            
            perfTracker.Stop();
            var totalTime = perfTracker.ElapsedMilliseconds;
            logger.LogResponse(operation, url, totalTime, new { Count = result?.Count ?? 0 });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiSourceResponse?> AddServerSourceAsync(string serverId, string source)
    {
        const string operation = "AddServerSource";
        var url = GoApiUrlBuilder.BuildAddServerSourceUrl(serverId);
        var request = new GoApiSourceRequest { Source = source };

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = GoApiJsonSerializer.DeserializeSourceResponse(content);
            
            perfTracker.Stop();
            var totalTime = perfTracker.ElapsedMilliseconds;
            logger.LogResponse(operation, url, totalTime, new { Source = source });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiSourceResponse?> AddServerSourceByKeyAsync(string serverKey, string source)
    {
        const string operation = "AddServerSourceByKey";
        var url = GoApiUrlBuilder.BuildAddServerSourceByKeyUrl(serverKey);
        var request = new GoApiSourceRequest { Source = source };

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = GoApiJsonSerializer.DeserializeSourceResponse(content);
            
            perfTracker.Stop();
            var totalTime = perfTracker.ElapsedMilliseconds;
            logger.LogResponse(operation, url, totalTime, new { Source = source });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersAsync(string serverId, GoApiSourceIdentifiersRequest request)
    {
        const string operation = "AddServerSourceIdentifiers";
        var url = GoApiUrlBuilder.BuildAddServerSourceIdentifiersUrl(serverId);

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = GoApiJsonSerializer.DeserializeSourceIdentifiersResponse(content);
            
            perfTracker.Stop();
            logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { request.SourceType });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersByKeyAsync(string serverKey, GoApiSourceIdentifiersRequest request)
    {
        const string operation = "AddServerSourceIdentifiersByKey";
        var url = GoApiUrlBuilder.BuildAddServerSourceIdentifiersByKeyUrl(serverKey);

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            // Log the exact JSON being sent for debugging
            var jsonRequest = GoApiJsonSerializer.SerializeForDebug(request);
            logger.LogDebug(operation, "JSON request to Go API", jsonRequest);

            var response = await httpHandler.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = GoApiJsonSerializer.DeserializeSourceIdentifiersResponse(content);
            
            perfTracker.Stop();
            logger.LogResponse(
                operation,
                url,
                perfTracker.ElapsedMilliseconds,
                new
                {
                    request.SourceType,
                    request.TelegramId
                });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }

    public async Task<List<GoApiServerInfo>?> FindServersByTelegramIdAsync(long telegramId)
    {
        const string operation = "FindServersByTelegramId";
        var url = GoApiUrlBuilder.BuildFindServersByTelegramIdUrl(telegramId);

        using var perfTracker = GoApiPerformanceTracker.Start();
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                if (GoApiHttpHandler.IsNotFound(response))
                {
                    logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { TelegramId = telegramId, Found = 0 });
                    return new List<GoApiServerInfo>();
                }

                var errorContent = await GoApiHttpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await GoApiHttpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var servers = GoApiJsonSerializer.DeserializeServersList(content);
            
            perfTracker.Stop();
            var totalTime = perfTracker.ElapsedMilliseconds;
            logger.LogResponse(
                operation,
                url,
                totalTime,
                new
                {
                    TelegramId = telegramId,
                    Count = servers?.Count ?? 0
                });

            return servers ?? new List<GoApiServerInfo>();
        }
        catch (GoApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw GoApiErrorHandler.MapException(ex);
        }
    }
}
