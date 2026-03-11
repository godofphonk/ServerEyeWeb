namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Interfaces.Services;

[Route("api/servers")]
[Authorize]
public class ServerMetricsController : BaseApiController
{
    private readonly IMetricsService metricsService;
    private readonly ILogger<ServerMetricsController> logger;

    public ServerMetricsController(IMetricsService metricsService, ILogger<ServerMetricsController> logger)
    {
        this.metricsService = metricsService;
        this.logger = logger;
    }

    [HttpGet("by-key/{serverKey}/metrics")]
    public async Task<ActionResult<RawMetricsResponse>> GetMetricsByKey(string serverKey, [FromQuery] MetricsRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            this.logger.LogInformation(
                "GetMetricsByKey called: ServerKey={ServerKey}, Start={Start}, End={End}, Granularity={Granularity}",
                serverKey,
                request.Start,
                request.End,
                request.Granularity);
            
            var userId = GetUserId();
            
            // Handle nullable parameters
            DateTime? start = request.Start;
            DateTime? end = request.End;
            string? granularity = request.Granularity;
            
            // If no parameters provided, use default behavior (last 5 minutes)
            if (!start.HasValue && !end.HasValue && string.IsNullOrEmpty(granularity))
            {
                end = DateTime.UtcNow;
                start = end.Value.AddMinutes(-5);
                granularity = "minute";
                this.logger.LogInformation("No parameters provided, using default: last 5 minutes with minute granularity");
            }
            else if (!start.HasValue || !end.HasValue)
            {
                // If some parameters are provided but start/end are missing, set defaults
                end ??= DateTime.UtcNow;
                start ??= end.Value.AddMinutes(-5);
                granularity ??= "minute";
                this.logger.LogInformation("Partial parameters provided, using defaults for missing values");
            }
        
            var metrics = await metricsService.GetMetricsByKeyAsync(userId, serverKey, start!.Value, end!.Value, granularity);
            return metrics;
        });
    }

    [HttpGet("{serverId}/metrics/tiered")]
    public async Task<ActionResult<RawMetricsResponse>> GetTieredMetrics(string serverId, [FromQuery] MetricsRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            this.logger.LogInformation(
                "GetTieredMetrics called: ServerId={ServerId}, Start={Start}, End={End}, Granularity={Granularity}",
                serverId,
                request.Start,
                request.End,
                request.Granularity);
            
            var userId = GetUserId();
            
            // Handle nullable parameters
            DateTime? start = request.Start;
            DateTime? end = request.End;
            string? granularity = request.Granularity;
            
            // If no parameters provided, use default behavior (like dashboard)
            if (!start.HasValue && !end.HasValue && string.IsNullOrEmpty(granularity))
            {
                this.logger.LogInformation("No parameters provided, using default behavior");
            }
        
            var metrics = await metricsService.GetMetricsAsync(userId, serverId, start, end, granularity);
            return metrics;
        });
    }

    [HttpGet("{serverId}/metrics/realtime")]
    public async Task<ActionResult<RawMetricsResponse>> GetRealtimeMetrics(string serverId, [FromQuery] string? duration = null)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var userId = GetUserId();
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

            var metrics = await metricsService.GetRealtimeMetricsAsync(userId, serverId, durationSpan);
            return metrics;
        });
    }

    [HttpGet("{serverId}/metrics/dashboard")]
    public async Task<ActionResult<RawMetricsResponse>> GetDashboardMetrics(string serverId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var userId = GetUserId();
            var metrics = await metricsService.GetDashboardMetricsAsync(userId, serverId);
            return metrics;
        });
    }
}
