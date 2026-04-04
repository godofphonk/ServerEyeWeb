namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowFrontend")]
public class MetricsController : ControllerBase
{
    private readonly ILogger<MetricsController> logger;

    public MetricsController(ILogger<MetricsController> logger) => this.logger = logger;

    [HttpGet("{serverId}/latest")]
    public ActionResult<List<MetricDto>> GetLatestMetrics(string serverId)
    {
        this.logger.LogDebug("Getting latest metrics for server: {ServerId}", serverId?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

        // Возвращаем тестовые метрики для указанного сервера
        var metrics = new List<MetricDto>
        {
            new MetricDto
            {
                ServerId = serverId ?? string.Empty,
                Type = "cpu_temperature",
                Value = 45.5,
                Unit = "°C",
                Timestamp = DateTime.UtcNow.AddMinutes(-1)
            },
            new MetricDto
            {
                ServerId = serverId ?? string.Empty,
                Type = "memory_usage",
                Value = 67.8,
                Unit = "%",
                Timestamp = DateTime.UtcNow.AddMinutes(-1)
            },
            new MetricDto
            {
                ServerId = serverId ?? string.Empty,
                Type = "disk_usage",
                Value = 82.3,
                Unit = "%",
                Timestamp = DateTime.UtcNow.AddMinutes(-1)
            }
        };

        this.logger.LogDebug("Returning {Count} metrics for server: {ServerId}", metrics.Count, serverId?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
        return this.Ok(metrics);
    }
}
