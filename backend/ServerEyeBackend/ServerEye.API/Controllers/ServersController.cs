namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using ServerEye.Core.DTOs;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowFrontend")]
public class ServersController : ControllerBase
{
    [HttpGet]
    public ActionResult<ServersResponseDto> GetServers()
    {
        // Возвращаем тестовые данные для начала
        var servers = new ServersResponseDto
        {
            Servers = new List<ServerDto>
            {
                new ServerDto
                {
                    Id = "server-uuid-1",
                    Name = "Main Server",
                    Hostname = "server.local",
                    IpAddress = "192.168.1.100",
                    Os = "Ubuntu 22.04",
                    Status = "online",
                    ApiKey = "api-key-123",
                    LastHeartbeat = DateTime.UtcNow.AddMinutes(-5),
                    Tags = new List<string> { "production", "web" }.AsReadOnly(),
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
                },
                new ServerDto
                {
                    Id = "server-uuid-2", 
                    Name = "Database Server",
                    Hostname = "db.local",
                    IpAddress = "192.168.1.101",
                    Os = "Ubuntu 20.04",
                    Status = "offline",
                    ApiKey = "api-key-456",
                    LastHeartbeat = DateTime.UtcNow.AddHours(-2),
                    Tags = new List<string> { "database", "backup" }.AsReadOnly(),
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2)
                }
            }.AsReadOnly()
        };

        return this.Ok(servers);
    }
}
