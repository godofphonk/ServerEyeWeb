namespace ServerEye.Infrastracture.ExternalServices;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Infrastracture.ExternalServices.GoApi;
using ServerEye.Core.Exceptions;

/// <summary>
/// Refactored Go API client with separated responsibilities.
/// </summary>
public class GoApiRefactored(
    GoApiHttpHandler httpHandler,
    GoApiJsonSerializer jsonSerializer,
    GoApiUrlBuilder urlBuilder,
    GoApiDataTransformer dataTransformer,
    GoApiErrorHandler errorHandler,
    GoApiPerformanceTracker performanceTracker,
    GoApiLogger logger) : IGoApiClient
{
    private readonly GoApiHttpHandler httpHandler = httpHandler;
    private readonly GoApiJsonSerializer jsonSerializer = jsonSerializer;
    private readonly GoApiUrlBuilder urlBuilder = urlBuilder;
    private readonly GoApiDataTransformer dataTransformer = dataTransformer;
    private readonly GoApiErrorHandler errorHandler = errorHandler;
    private readonly GoApiLogger logger = logger;

    public async Task<GoApiMetricsResponse?> GetMetricsByKeyAsync(string serverKey, DateTime start, DateTime endTime, string? granularity = null)
    {
        const string operation = "GetMetricsByKey";
        var url = urlBuilder.BuildMetricsByKeyUrl(serverKey, start, endTime, granularity);

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);
            var requestTime = perfTracker.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, requestTime, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
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
            var result = jsonSerializer.DeserializeMetricsResponse(content);
            
            // If no data points, try snapshot format
            if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
            {
                var snapshotResponse = jsonSerializer.DeserializeSnapshotResponse(content);
                if (snapshotResponse != null && snapshotResponse.Metrics != null)
                {
                    result = dataTransformer.ConvertSnapshotToTimeSeries(snapshotResponse, start, endTime, granularity);
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
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<GoApiMetricsResponse?> GetMetricsAsync(string serverId, DateTime start, DateTime endTime, string? granularity = null)
    {
        const string operation = "GetMetrics";
        var url = urlBuilder.BuildMetricsUrl(serverId, start, endTime, granularity);

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);
            var requestTime = perfTracker.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, requestTime, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = jsonSerializer.DeserializeMetricsResponse(content);
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
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<GoApiMetricsResponse?> GetRealtimeMetricsAsync(string serverId, TimeSpan? duration = null)
    {
        try
        {
            var actualDuration = duration ?? TimeSpan.FromMinutes(5);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(actualDuration);

            logger.LogRequest("GetRealtimeMetrics", $"Server: {serverId}, Duration: {actualDuration}");

            return await GetMetricsAsync(serverId, startTime, endTime);
        }
        catch (Exception ex)
        {
            logger.LogException("GetRealtimeMetrics", $"Server: {serverId}", 0, ex);
            throw errorHandler.MapException(ex, "GetRealtimeMetrics", $"Server: {serverId}");
        }
    }

    public async Task<GoApiServerInfo?> ValidateServerKeyAsync(string serverKey)
    {
        const string operation = "ValidateServerKey";
        var url = urlBuilder.BuildServerValidationUrl(serverKey);

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                
                if (httpHandler.IsNotFound(response))
                {
                    return null;
                }
                
                throw errorHandler.CreateServerKeyValidationException(response.StatusCode, errorContent);
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var metricsResponse = jsonSerializer.DeserializeMetricsResponse(content);

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
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<GoApiStaticInfo?> GetStaticInfoAsync(string serverKey)
    {
        const string operation = "GetStaticInfo";
        var url = urlBuilder.BuildStaticInfoUrl(serverKey);

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var goApiResponse = jsonSerializer.DeserializeStaticInfoResponse(content);
            if (goApiResponse == null)
            {
                return null;
            }

            var result = dataTransformer.ConvertToStaticInfo(goApiResponse);
            
            perfTracker.Stop();
            logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { ServerId = result.ServerId });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<GoApiServerInfo?> GetServerInfoAsync(string serverId)
    {
        const string operation = "GetServerInfo";
        var url = urlBuilder.BuildServerInfoUrl(serverId);

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = jsonSerializer.DeserializeServerInfo(content);
            
            perfTracker.Stop();
            logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { ServerId = result?.ServerId });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<GoApiMetricsResponse?> GetDashboardMetricsAsync(string serverId)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-5);

            logger.LogRequest("GetDashboardMetrics", $"Server: {serverId} (last 5 minutes)");
            
            return await GetMetricsAsync(serverId, startTime, endTime);
        }
        catch (Exception ex)
        {
            logger.LogException("GetDashboardMetrics", $"Server: {serverId}", 0, ex);
            throw errorHandler.MapException(ex, "GetDashboardMetrics", $"Server: {serverId}");
        }
    }

    public async Task<List<GoApiServerInfo>?> GetServersListAsync()
    {
        const string operation = "GetServersList";
        var url = urlBuilder.BuildServersListUrl();

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = jsonSerializer.DeserializeServersList(content);
            
            perfTracker.Stop();
            logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { Count = result?.Count ?? 0 });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<GoApiSourceResponse?> AddServerSourceAsync(string serverId, string source)
    {
        const string operation = "AddServerSource";
        var url = urlBuilder.BuildAddServerSourceUrl(serverId);
        var request = new GoApiSourceRequest { Source = source };

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = jsonSerializer.DeserializeSourceResponse(content);
            
            perfTracker.Stop();
            logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { Source = source });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<GoApiSourceResponse?> AddServerSourceByKeyAsync(string serverKey, string source)
    {
        const string operation = "AddServerSourceByKey";
        var url = urlBuilder.BuildAddServerSourceByKeyUrl(serverKey);
        var request = new GoApiSourceRequest { Source = source };

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = jsonSerializer.DeserializeSourceResponse(content);
            
            perfTracker.Stop();
            logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { Source = source });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersAsync(string serverId, GoApiSourceIdentifiersRequest request)
    {
        const string operation = "AddServerSourceIdentifiers";
        var url = urlBuilder.BuildAddServerSourceIdentifiersUrl(serverId);

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = jsonSerializer.DeserializeSourceIdentifiersResponse(content);
            
            perfTracker.Stop();
            logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { SourceType = request.SourceType });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersByKeyAsync(string serverKey, GoApiSourceIdentifiersRequest request)
    {
        const string operation = "AddServerSourceIdentifiersByKey";
        var url = urlBuilder.BuildAddServerSourceIdentifiersByKeyUrl(serverKey);

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            // Log the exact JSON being sent for debugging
            var jsonRequest = jsonSerializer.SerializeForDebug(request);
            logger.LogDebug(operation, "JSON request to Go API", jsonRequest);

            var response = await httpHandler.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var result = jsonSerializer.DeserializeSourceIdentifiersResponse(content);
            
            perfTracker.Stop();
            logger.LogResponse(
                operation,
                url,
                perfTracker.ElapsedMilliseconds,
                new
                {
                    SourceType = request.SourceType,
                    TelegramId = request.TelegramId
                });

            return result;
        }
        catch (Exception ex)
        {
            perfTracker.Stop();
            logger.LogException(operation, url, perfTracker.ElapsedMilliseconds, ex);
            throw errorHandler.MapException(ex, operation, url);
        }
    }

    public async Task<List<GoApiServerInfo>?> FindServersByTelegramIdAsync(long telegramId)
    {
        const string operation = "FindServersByTelegramId";
        var url = urlBuilder.BuildFindServersByTelegramIdUrl(telegramId);

        using var perfTracker = GoApiPerformanceTracker.Start(operation, url);
        
        try
        {
            logger.LogRequest(operation, url);

            var response = await httpHandler.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                if (httpHandler.IsNotFound(response))
                {
                    logger.LogResponse(operation, url, perfTracker.ElapsedMilliseconds, new { TelegramId = telegramId, Found = 0 });
                    return new List<GoApiServerInfo>();
                }

                var errorContent = await httpHandler.GetErrorContentAsync(response);
                logger.LogError(operation, url, perfTracker.ElapsedMilliseconds, (int)response.StatusCode, errorContent);
                return null;
            }

            var content = await httpHandler.GetSuccessfulResponseContentAsync(response);
            if (content == null)
            {
                return null;
            }

            var servers = jsonSerializer.DeserializeServersList(content);
            
            perfTracker.Stop();
            logger.LogResponse(
                operation,
                url,
                perfTracker.ElapsedMilliseconds,
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
            throw errorHandler.MapException(ex, operation, url);
        }
    }
}
