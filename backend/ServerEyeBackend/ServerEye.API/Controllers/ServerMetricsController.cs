namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Interfaces.Services;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/servers/{serverId}/metrics")]
public class ServerMetricsController : ControllerBase
{
    private readonly IMetricsService metricsService;
    private readonly ILogger<ServerMetricsController> logger;

    public ServerMetricsController(IMetricsService metricsService, ILogger<ServerMetricsController> logger)
    {
        this.metricsService = metricsService;
        this.logger = logger;
    }

    [HttpGet("tiered")]
    public async Task<ActionResult<RawMetricsResponse>> GetTieredMetrics(string serverId, [FromQuery] MetricsRequest request)
    {
        try
        {
            this.logger.LogInformation(
                "GetTieredMetrics called: ServerId={ServerId}, Start={Start}, End={End}, Granularity={Granularity}",
                serverId,
                request.Start,
                request.End,
                request.Granularity);
            
            var userId = this.GetUserId();
            
            // Handle nullable parameters
            DateTime? start = request.Start;
            DateTime? end = request.End;
            string? granularity = request.Granularity;
            
            // If no parameters provided, use default behavior (like dashboard)
            if (!start.HasValue && !end.HasValue && string.IsNullOrEmpty(granularity))
            {
                this.logger.LogInformation("No parameters provided, using default behavior");
            }
            
            var metrics = await this.metricsService.GetMetricsAsync(userId, serverId, start, end, granularity);
            return this.Ok(metrics);
        }
        catch (UnauthorizedAccessException ex)
        {
            this.logger.LogWarning(ex, "Unauthorized access to metrics");
            return this.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning(ex, "Invalid operation while getting metrics");
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting tiered metrics");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("realtime")]
    public async Task<ActionResult<RawMetricsResponse>> GetRealtimeMetrics(string serverId, [FromQuery] string? duration = null)
    {
        try
        {
            var userId = this.GetUserId();
            TimeSpan? durationSpan = null;

            if (!string.IsNullOrEmpty(duration))
            {
                if (duration.EndsWith('m'))
                {
                    var minutes = int.Parse(duration.TrimEnd('m'));
                    durationSpan = TimeSpan.FromMinutes(minutes);
                }
                else if (duration.EndsWith('h'))
                {
                    var hours = int.Parse(duration.TrimEnd('h'));
                    durationSpan = TimeSpan.FromHours(hours);
                }
            }

            var metrics = await this.metricsService.GetRealtimeMetricsAsync(userId, serverId, durationSpan);
            return this.Ok(metrics);
        }
        catch (UnauthorizedAccessException ex)
        {
            this.logger.LogWarning(ex, "Unauthorized access to realtime metrics");
            return this.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning(ex, "Invalid operation while getting realtime metrics");
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting realtime metrics");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<RawMetricsResponse>> GetDashboardMetrics(string serverId)
    {
        try
        {
            var userId = this.GetUserId();
            var metrics = await this.metricsService.GetDashboardMetricsAsync(userId, serverId);
            return this.Ok(metrics);
        }
        catch (UnauthorizedAccessException ex)
        {
            this.logger.LogWarning(ex, "Unauthorized access to dashboard metrics");
            return this.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning(ex, "Invalid operation while getting dashboard metrics");
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting dashboard metrics");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }

        return userId;
    }
}
