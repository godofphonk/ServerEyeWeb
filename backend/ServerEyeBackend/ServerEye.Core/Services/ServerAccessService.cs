namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;
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
    private readonly IUserExternalLoginRepository externalLoginRepository;
    private readonly IGoApiClient goApiClient;
    private readonly IEncryptionService encryptionService;
    private readonly ILogger<ServerAccessService> logger;

    public ServerAccessService(
        IMonitoredServerRepository serverRepository,
        IUserServerAccessRepository accessRepository,
        IUserRepository userRepository,
        IUserExternalLoginRepository externalLoginRepository,
        IGoApiClient goApiClient,
        IEncryptionService encryptionService,
        ILogger<ServerAccessService> logger)
    {
        this.serverRepository = serverRepository;
        this.accessRepository = accessRepository;
        this.userRepository = userRepository;
        this.externalLoginRepository = externalLoginRepository;
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

            // Decrypt ServerKey for frontend
            var decryptedKey = string.IsNullOrEmpty(server.ServerKey) 
                ? string.Empty 
                : this.encryptionService.Decrypt(server.ServerKey);

            result.Add(new ServerResponse
            {
                Id = server.Id,
                ServerId = server.ServerId,
                ServerKey = decryptedKey,
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

        // Get telegram_id if user has Telegram OAuth linked
        var telegramId = await this.GetUserTelegramIdAsync(userId);

        var existingServer = await this.serverRepository.GetByServerIdAsync(serverInfo.ServerId);

        if (existingServer != null)
        {
            var hasAccess = await this.accessRepository.HasAccessAsync(userId, serverInfo.ServerId);
            if (hasAccess)
            {
                throw new InvalidOperationException("Server already added to your account");
            }

            // For existing servers, only add user identifier to existing Web source
            var identifiersRequest = new GoApiSourceIdentifiersRequest
            {
                SourceType = "Web",
                Identifiers = new List<string> { userId.ToString() },
                IdentifierType = "user_id",
                TelegramId = telegramId,
                Metadata = new Dictionary<string, object>
                {
                    { "added_at", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                    { "source", "ServerEyeWeb" }
                }
            };

            if (telegramId.HasValue)
            {
                identifiersRequest.Metadata!["telegram_linked_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            var identifiersResponse = await this.goApiClient.AddServerSourceIdentifiersByKeyAsync(serverKey, identifiersRequest);
            if (identifiersResponse == null)
            {
                this.logger.LogWarning("Failed to add user identifier to Go API for existing server key {ServerKey}", serverKey);
            }
            else
            {
                this.logger.LogInformation("Successfully added user identifier to Go API for existing server {ServerId}", identifiersResponse.ServerId);
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
                ServerKey = this.encryptionService.Decrypt(existingServer.ServerKey),
                Hostname = existingServer.Hostname,
                OperatingSystem = existingServer.OperatingSystem,
                AccessLevel = AccessLevel.Viewer,
                AddedAt = DateTime.UtcNow,
                LastSeen = existingServer.LastSeen,
                IsActive = existingServer.IsActive
            };
        }

        // For new servers, add both source and identifiers
        var sourceResponse = await this.goApiClient.AddServerSourceByKeyAsync(serverKey, "Web");
        if (sourceResponse == null)
        {
            this.logger.LogWarning("Failed to add Web source to Go API for server key {ServerKey}", serverKey);
        }
        else
        {
            this.logger.LogInformation("Successfully added Web source to Go API for server {ServerId}", sourceResponse.ServerId);
            
            // Add user identifier to the Web source
            var identifiersRequest = new GoApiSourceIdentifiersRequest
            {
                SourceType = "Web",
                Identifiers = new List<string> { userId.ToString() },
                IdentifierType = "user_id",
                TelegramId = telegramId,
                Metadata = new Dictionary<string, object>
                {
                    { "added_at", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                    { "source", "ServerEyeWeb" }
                }
            };

            if (telegramId.HasValue)
            {
                identifiersRequest.Metadata!["telegram_linked_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            var identifiersResponse = await this.goApiClient.AddServerSourceIdentifiersByKeyAsync(serverKey, identifiersRequest);
            if (identifiersResponse == null)
            {
                this.logger.LogWarning("Failed to add user identifier to Go API for server key {ServerKey}", serverKey);
            }
            else
            {
                this.logger.LogInformation("Successfully added user identifier to Go API for server {ServerId}", identifiersResponse.ServerId);
            }
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
            ServerKey = serverKey, // Return original key for new servers
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

            this.logger.LogInformation("User {UserId} shared server {ServerId} with {TargetUserEmail} at level {AccessLevel}", ownerId, serverId, targetUserEmail, level);
        }
    }

    private async Task<long?> GetUserTelegramIdAsync(Guid userId)
    {
        this.logger.LogInformation("Getting telegram_id for user {UserId}", userId);
        
        var telegramLogin = await this.externalLoginRepository.GetByUserIdAndProviderAsync(userId, OAuthProvider.Telegram);
        
        if (telegramLogin == null)
        {
            this.logger.LogWarning("No Telegram OAuth found for user {UserId}", userId);
            return null;
        }

        this.logger.LogInformation("Found Telegram OAuth for user {UserId}: ProviderUserId = {ProviderUserId}", userId, telegramLogin.ProviderUserId);

        if (long.TryParse(telegramLogin.ProviderUserId, out var telegramId))
        {
            this.logger.LogInformation("Successfully parsed telegram_id {TelegramId} for user {UserId}", telegramId, userId);
            return telegramId;
        }

        this.logger.LogWarning("Failed to parse telegram_id for user {UserId}: {ProviderUserId}", userId, telegramLogin.ProviderUserId);
        return null;
    }
}
