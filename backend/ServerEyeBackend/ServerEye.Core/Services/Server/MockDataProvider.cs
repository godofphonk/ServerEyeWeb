namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs;
using ServerEye.Core.Interfaces.Services;

public class MockDataProvider(ILogger<MockDataProvider> logger) : IMockDataProvider
{
    private readonly ILogger<MockDataProvider> logger = logger;

    public async Task<ServersResponseDto> GetMockServersAsync()
    {
        logger.LogInformation("Returning mock servers data from provider");

        await Task.Delay(1); // Simulate async operation

        var mockServers = new List<ServerDto>
        {
            new ServerDto
            {
                Id = "server-uuid-1",
                Name = "Main Server",
                Hostname = "server.local",
                IpAddress = "192.168.1.100",
                Os = "Ubuntu 22.04",
                Status = "online",
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
                LastHeartbeat = DateTime.UtcNow.AddHours(-2),
                Tags = new List<string> { "database", "backup" }.AsReadOnly(),
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        return new ServersResponseDto
        {
            Servers = mockServers.AsReadOnly()
        };
    }
}
