namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using ServerEye.Core.DTOs;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowFrontend")]
public class MetricsController : ControllerBase
{
    [HttpGet("{serverId}/latest")]
    public ActionResult<List<MetricDto>> GetLatestMetrics(string serverId)
    {
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

        return this.Ok(metrics);
    }
}
