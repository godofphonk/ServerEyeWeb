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
            this.logger.LogInformation("GetMetricsByKey called: ServerKey={ServerKey}", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

            var userId = GetUserId();
            this.logger.LogInformation("GetMetricsByKey: UserId={UserId}", userId);

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

            this.logger.LogInformation(
                "GetMetricsByKey: Parameters processed - Start={Start}, End={End}, Granularity={Granularity}",
                start,
                end,
                granularity?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

            var metrics = await metricsService.GetMetricsByKeyAsync(userId, serverKey ?? string.Empty, start!.Value, end!.Value, granularity);
            return metrics;
        });
    }

    [HttpGet("by-key/{serverKey}/metrics/tiered")]
    public async Task<ActionResult<RawMetricsResponse>> GetTieredMetricsByKey(string serverKey, [FromQuery] MetricsRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            this.logger.LogInformation("GetTieredMetricsByKey called: ServerKey={ServerKey}", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

            var userId = GetUserId();

            // Handle nullable parameters - tiered endpoint requires start and end
            DateTime? start = request.Start;
            DateTime? end = request.End;
            string? granularity = request.Granularity;

            // If no parameters provided, use default behavior (last 1 hour for graphs)
            if (!start.HasValue || !end.HasValue)
            {
                end = DateTime.UtcNow;
                start = end.Value.AddHours(-1);
                granularity = "1m"; // Default granularity for 1 hour
                this.logger.LogInformation("No time range provided, using default: last 1 hour with 1m granularity");
            }

            this.logger.LogInformation(
                "GetTieredMetricsByKey: Parameters - Start={Start}, End={End}, Granularity={Granularity}",
                start,
                end,
                granularity?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

            var metrics = await metricsService.GetTieredMetricsByKeyAsync(userId, serverKey ?? string.Empty, start!.Value, end!.Value, granularity);
            return metrics;
        });
    }
}
