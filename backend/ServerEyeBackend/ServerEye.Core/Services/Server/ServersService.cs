namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs;
using ServerEye.Core.Interfaces.Services;

public class ServersService(
    IServerAccessService serverAccessService,
    IMockDataProvider mockDataProvider,
    ServersConfiguration configuration,
    ILogger<ServersService> logger) : IServersService
{
    private readonly IServerAccessService serverAccessService = serverAccessService;
    private readonly IMockDataProvider mockDataProvider = mockDataProvider;
    private readonly ServersConfiguration configuration = configuration;
    private readonly ILogger<ServersService> logger = logger;

    public async Task<ServersResponseDto> GetUserServersAsync(Guid userId)
    {
        try
        {
            if (configuration.EnableDetailedLogging)
            {
                logger.LogInformation("Getting servers for user {UserId}", userId);
            }

            var userServers = await serverAccessService.GetUserServersAsync(userId);

            if (userServers.Count >= configuration.MaxServersPerUser)
            {
                logger.LogWarning("User {UserId} has reached maximum server limit", userId);
                throw new InvalidOperationException("Maximum server limit reached");
            }

            var serverDtos = userServers.Select(server => new ServerDto
            {
                Id = server.Id.ToString(),
                Name = server.Hostname,
                Hostname = server.Hostname,
                IpAddress = string.IsNullOrEmpty(server.IpAddress) ? "127.0.0.1" : server.IpAddress,
                Os = server.OperatingSystem,
                Status = server.IsActive ? "online" : "offline",
                LastHeartbeat = server.LastSeen ?? DateTime.UtcNow,
                Tags = new List<string> { server.AccessLevel.ToString() }.AsReadOnly(),
                CreatedAt = server.AddedAt,
                UpdatedAt = server.LastSeen ?? DateTime.UtcNow
            }).ToList();

            if (configuration.EnableDetailedLogging)
            {
                logger.LogInformation("Successfully retrieved {Count} servers for user {UserId}", serverDtos.Count, userId);
            }

            return new ServersResponseDto
            {
                Servers = serverDtos.AsReadOnly()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting servers for user {UserId}", userId);
            throw; // Let global exception handler create generic response
        }
    }

    public async Task<ServersResponseDto> GetMockServersAsync()
    {
        if (configuration.EnableDetailedLogging)
        {
            logger.LogInformation("Returning mock servers data from service");
        }

        await Task.Delay(configuration.MockDataDelayMs);

        return await mockDataProvider.GetMockServersAsync();
    }
}
