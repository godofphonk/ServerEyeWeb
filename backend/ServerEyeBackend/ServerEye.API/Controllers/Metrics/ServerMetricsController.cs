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
