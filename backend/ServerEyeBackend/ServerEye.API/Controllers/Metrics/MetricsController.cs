namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
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
        this.logger.LogDebug("Getting latest metrics for server: {ServerId}", serverId);

        // Возвращаем тестовые метрики для указанного сервера
        var metrics = new List<MetricDto>
        {
            new MetricDto
            {
                ServerId = serverId,
                Type = "cpu_temperature",
                Value = 45.5,
                Unit = "°C",
                Timestamp = DateTime.UtcNow.AddMinutes(-1)
            },
            new MetricDto
            {
                ServerId = serverId,
                Type = "memory_usage",
                Value = 67.8,
                Unit = "%",
                Timestamp = DateTime.UtcNow.AddMinutes(-1)
            },
            new MetricDto
            {
                ServerId = serverId,
                Type = "disk_usage",
                Value = 82.3,
                Unit = "%",
                Timestamp = DateTime.UtcNow.AddMinutes(-1)
            }
        };

        this.logger.LogDebug("Returning {Count} metrics for server: {ServerId}", metrics.Count, serverId);
        return this.Ok(metrics);
    }
}
