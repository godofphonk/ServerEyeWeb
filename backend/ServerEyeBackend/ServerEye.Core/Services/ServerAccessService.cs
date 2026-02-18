namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class ServerAccessService : IServerAccessService
{
    private readonly IMonitoredServerRepository serverRepository;
    private readonly IUserServerAccessRepository accessRepository;
    private readonly IUserRepository userRepository;
    private readonly IGoApiClient goApiClient;
    private readonly IEncryptionService encryptionService;
    private readonly ILogger<ServerAccessService> logger;

    public ServerAccessService(
        IMonitoredServerRepository serverRepository,
        IUserServerAccessRepository accessRepository,
        IUserRepository userRepository,
        IGoApiClient goApiClient,
        IEncryptionService encryptionService,
        ILogger<ServerAccessService> logger)
    {
        this.serverRepository = serverRepository;
        this.accessRepository = accessRepository;
        this.userRepository = userRepository;
        this.goApiClient = goApiClient;
        this.encryptionService = encryptionService;
        this.logger = logger;
    }

    public async Task<bool> HasAccessAsync(Guid userId, string serverId)
    {
        return await this.accessRepository.HasAccessAsync(userId, serverId);
    }

    public async Task<AccessLevel?> GetAccessLevelAsync(Guid userId, string serverId)
    {
        return await this.accessRepository.GetAccessLevelAsync(userId, serverId);
    }

    public async Task<List<ServerResponse>> GetUserServersAsync(Guid userId)
    {
        var servers = await this.accessRepository.GetUserServersAsync(userId);
        var result = new List<ServerResponse>();

        foreach (var server in servers)
        {
            var accessLevel = await this.accessRepository.GetAccessLevelAsync(userId, server.ServerId);

            result.Add(new ServerResponse
            {
                Id = server.Id,
                ServerId = server.ServerId,
                Hostname = server.Hostname,
                OperatingSystem = server.OperatingSystem,
                AccessLevel = accessLevel ?? AccessLevel.Viewer,
                AddedAt = server.CreatedAt,
                LastSeen = server.LastSeen,
                IsActive = server.IsActive
            });
        }

        return result;
    }

    public async Task<ServerResponse> AddServerAsync(Guid userId, string serverKey)
    {
        var serverInfo = await this.goApiClient.ValidateServerKeyAsync(serverKey) ?? throw new InvalidOperationException("Invalid server key");

        var existingServer = await this.serverRepository.GetByServerIdAsync(serverInfo.ServerId);

        if (existingServer != null)
        {
            var hasAccess = await this.accessRepository.HasAccessAsync(userId, serverInfo.ServerId);
            if (hasAccess)
            {
                throw new InvalidOperationException("Server already added to your account");
            }

            await this.accessRepository.AddAccessAsync(new UserServerAccess
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ServerId = existingServer.Id,
                AccessLevel = AccessLevel.Viewer,
                AddedAt = DateTime.UtcNow
            });

            this.logger.LogInformation("User {UserId} added access to existing server {ServerId}", userId, serverInfo.ServerId);

            return new ServerResponse
            {
                Id = existingServer.Id,
                ServerId = existingServer.ServerId,
                Hostname = existingServer.Hostname,
                OperatingSystem = existingServer.OperatingSystem,
                AccessLevel = AccessLevel.Viewer,
                AddedAt = DateTime.UtcNow,
                LastSeen = existingServer.LastSeen,
                IsActive = existingServer.IsActive
            };
        }

        var encryptedKey = this.encryptionService.Encrypt(serverKey);

        var newServer = new Server
        {
            Id = Guid.NewGuid(),
            ServerId = serverInfo.ServerId,
            ServerKey = encryptedKey,
            Hostname = serverInfo.Hostname,
            OperatingSystem = serverInfo.OperatingSystem,
            AgentVersion = serverInfo.AgentVersion,
            CreatedAt = DateTime.UtcNow,
            LastSeen = serverInfo.LastSeen,
            IsActive = true
        };

        await this.serverRepository.AddAsync(newServer);

        await this.accessRepository.AddAccessAsync(new UserServerAccess
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServerId = newServer.Id,
            AccessLevel = AccessLevel.Owner,
            AddedAt = DateTime.UtcNow
        });

        this.logger.LogInformation("User {UserId} added new server {ServerId} as owner", userId, serverInfo.ServerId);

        return new ServerResponse
        {
            Id = newServer.Id,
            ServerId = newServer.ServerId,
            Hostname = newServer.Hostname,
            OperatingSystem = newServer.OperatingSystem,
            AccessLevel = AccessLevel.Owner,
            AddedAt = DateTime.UtcNow,
            LastSeen = newServer.LastSeen,
            IsActive = newServer.IsActive
        };
    }

    public async Task RemoveServerAsync(Guid userId, string serverId)
    {
        var hasAccess = await this.accessRepository.HasAccessAsync(userId, serverId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("You don't have access to this server");
        }

        await this.accessRepository.RemoveAccessAsync(userId, serverId);

        this.logger.LogInformation("User {UserId} removed access to server {ServerId}", userId, serverId);
    }

    public async Task ShareServerAsync(Guid ownerId, string serverId, string targetUserEmail, AccessLevel level)
    {
        var ownerAccessLevel = await this.accessRepository.GetAccessLevelAsync(ownerId, serverId);
        if (ownerAccessLevel != AccessLevel.Owner)
        {
            throw new UnauthorizedAccessException("Only owner can share server");
        }

        var targetUser = await this.userRepository.GetByEmailAsync(targetUserEmail) ?? throw new InvalidOperationException("Target user not found");

        var server = await this.serverRepository.GetByServerIdAsync(serverId) ?? throw new InvalidOperationException("Server not found");

        var targetHasAccess = await this.accessRepository.HasAccessAsync(targetUser.Id, serverId);
        if (targetHasAccess)
        {
            await this.accessRepository.UpdateAccessLevelAsync(targetUser.Id, serverId, level);
            this.logger.LogInformation("Updated access level for user {UserId} to server {ServerId}", targetUser.Id, serverId);
        }
        else
        {
            await this.accessRepository.AddAccessAsync(new UserServerAccess
            {
                Id = Guid.NewGuid(),
                UserId = targetUser.Id,
                ServerId = server.Id,
                AccessLevel = level,
                AddedAt = DateTime.UtcNow
            });

            this.logger.LogInformation("Shared server {ServerId} with user {UserId}", serverId, targetUser.Id);
        }
    }
}
