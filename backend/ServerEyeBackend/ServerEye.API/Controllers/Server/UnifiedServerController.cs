namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.API.DTOs;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Interfaces.Services;

/// <summary>
/// Unified controller for server metrics, status, and static info.
/// </summary>
[ApiController]
[Route("api/servers/by-key/{serverKey}")]
[Authorize]
public class UnifiedServerController : ControllerBase
{
    private readonly IMetricsService metricsService;
    private readonly IStaticInfoService staticInfoService;
    private readonly ILogger<UnifiedServerController> logger;

    public UnifiedServerController(
        IMetricsService metricsService,
        IStaticInfoService staticInfoService,
        ILogger<UnifiedServerController> logger)
    {
        this.metricsService = metricsService;
        this.staticInfoService = staticInfoService;
        this.logger = logger;
    }

    /// <summary>
    /// Get unified server data combining metrics, status, and static info.
    /// </summary>
    /// <param name="serverKey">Server key identifier.</param>
    /// <param name="request">Unified request parameters.</param>
    /// <returns>Combined server data.</returns>
    [HttpGet("unified")]
    public async Task<ActionResult<GoApiUnifiedResponse>> GetUnifiedData(
        string serverKey,
        [FromQuery] UnifiedRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user identifier");
            }

            this.logger.LogInformation(
                "GetUnifiedData called: ServerKey={ServerKey}, UserId={UserId}, IncludeMetrics={IncludeMetrics}, IncludeStatus={IncludeStatus}, IncludeStatic={IncludeStatic}",
                serverKey,
                userGuid,
                request.IncludeMetrics,
                request.IncludeStatus,
                request.IncludeStatic);

            var response = new GoApiUnifiedResponse
            {
                ServerKey = serverKey
            };

            // Handle metrics request
            if (request.IncludeMetrics)
            {
                try
                {
                    // Set default time range if not provided
                    DateTime? start = request.Start;
                    DateTime? end = request.End;
                    string? granularity = request.Granularity;

                    if (!start.HasValue || !end.HasValue)
                    {
                        end = DateTime.UtcNow;
                        start = end.Value.AddMinutes(-5); // Default: last 5 minutes
                        granularity ??= "minute";
                    }

                    var metrics = await metricsService.GetMetricsByKeyAsync(userGuid, serverKey, start.Value, end.Value, granularity);

                    // Create new response with metrics
                    response = new GoApiUnifiedResponse
                    {
                        ServerKey = serverKey,
                        Metrics = metrics,
                        Status = response.Status,
                        StaticInfo = response.StaticInfo,
                        Timestamp = response.Timestamp
                    };
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error retrieving metrics for server {ServerKey}", serverKey);

                    // Continue without metrics rather than failing the entire request
                }
            }

            // Handle status request
            if (request.IncludeStatus)
            {
                try
                {
                    // For now, we can derive status from metrics or create a separate status service
                    // This would need to be implemented based on your status logic
                    // response.Status = await statusService.GetServerStatusAsync(userGuid, serverKey);

                    // Temporary: create basic status from metrics if available
                    if (response.Metrics?.DataPoints?.Count > 0)
                    {
                        var latestPoint = response.Metrics.DataPoints.LastOrDefault();

                        // Create new response with status
                        response = new GoApiUnifiedResponse
                        {
                            ServerKey = serverKey,
                            Metrics = response.Metrics,
                            Status = new GoApiServerStatus
                            {
                                Online = true,
                                LastSeen = latestPoint?.Timestamp ?? DateTime.UtcNow
                            },
                            StaticInfo = response.StaticInfo,
                            Timestamp = response.Timestamp
                        };
                    }
                    else
                    {
                        // Create new response with offline status
                        response = new GoApiUnifiedResponse
                        {
                            ServerKey = serverKey,
                            Metrics = response.Metrics,
                            Status = new GoApiServerStatus
                            {
                                Online = false,
                                LastSeen = DateTime.UtcNow
                            },
                            StaticInfo = response.StaticInfo,
                            Timestamp = response.Timestamp
                        };
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error retrieving status for server {ServerKey}", serverKey);

                    // Continue without status
                }
            }

            // Handle static info request
            if (request.IncludeStatic)
            {
                try
                {
                    var staticInfo = await staticInfoService.GetStaticInfoAsync(userGuid, serverKey);

                    // Create new response with static info
                    response = new GoApiUnifiedResponse
                    {
                        ServerKey = serverKey,
                        Metrics = response.Metrics,
                        Status = response.Status,
                        StaticInfo = staticInfo,
                        Timestamp = response.Timestamp
                    };
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error retrieving static info for server {ServerKey}", serverKey);

                    // Continue without static info
                }
            }

            this.logger.LogInformation("GetUnifiedData completed successfully for server {ServerKey}", serverKey);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            this.logger.LogWarning("Unauthorized access attempt for server key {ServerKey}: {Message}", serverKey, ex.Message);
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in GetUnifiedData for server key {ServerKey}", serverKey);
            return StatusCode(500, "Internal server error");
        }
    }
}
