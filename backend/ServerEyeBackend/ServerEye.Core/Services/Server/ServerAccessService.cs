namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Helpers;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class ServerAccessService(
    IMonitoredServerRepository serverRepository,
    IUserServerAccessRepository accessRepository,
    IUserRepository userRepository,
    IUserExternalLoginRepository externalLoginRepository,
    IGoApiClient goApiClient,
    IEncryptionService encryptionService,
    ILogger<ServerAccessService> logger)
    : IServerAccessService
{
    public async Task<bool> HasAccessAsync(Guid userId, string serverId) => await accessRepository.HasAccessAsync(userId, serverId);

    public async Task<AccessLevel?> GetAccessLevelAsync(Guid userId, string serverId) => await accessRepository.GetAccessLevelAsync(userId, serverId);

    public async Task<List<ServerResponse>> GetUserServersAsync(Guid userId)
    {
        var servers = await accessRepository.GetUserServersAsync(userId);
        var result = new List<ServerResponse>();

        foreach (var server in servers)
        {
            var accessLevel = await accessRepository.GetAccessLevelAsync(userId, server.ServerId ?? string.Empty);

            // Decrypt ServerKey for frontend
            string decryptedKey;
            try
            {
                decryptedKey = string.IsNullOrEmpty(server.ServerKey)
                    ? string.Empty
                    : encryptionService.Decrypt(server.ServerKey);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                // Key was encrypted with old encryption key, return empty for now
                logger.LogWarning("ServerKey for server {ServerId} could not be decrypted - possibly encrypted with old key", LogSanitizer.Sanitize(server.ServerId));
                decryptedKey = string.Empty;
            }

            result.Add(new ServerResponse
            {
                Id = server.Id,
                ServerId = server.ServerId ?? string.Empty,
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
        logger.LogInformation("User {UserId} attempting to add server with key", userId);

        var serverInfo = await goApiClient.ValidateServerKeyAsync(serverKey) ?? throw new InvalidOperationException("Invalid server key");

        logger.LogInformation("Server key validated successfully: {ServerId}", serverInfo.ServerId);

        // Get telegram_id if user has Telegram OAuth linked
        var telegramId = await this.GetUserTelegramIdAsync(userId);

        var existingServer = await serverRepository.GetByServerIdAsync(serverInfo.ServerId);

        if (existingServer != null)
        {
            var hasAccess = await accessRepository.HasAccessAsync(userId, serverInfo.ServerId);
            if (hasAccess)
            {
                logger.LogWarning("User {UserId} attempted to add server {ServerId} that's already in their account", userId, LogSanitizer.Sanitize(serverInfo.ServerId));
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

            var identifiersResponse = await goApiClient.AddServerSourceIdentifiersByKeyAsync(serverKey, identifiersRequest);
            if (identifiersResponse == null)
            {
                logger.LogWarning("Failed to add user identifier to Go API for existing server key {ServerKey}", LogSanitizer.MaskServerKey(serverKey));
            }
            else
            {
                logger.LogInformation("Successfully added user identifier to Go API for existing server {ServerId}", LogSanitizer.Sanitize(identifiersResponse.ServerId));
            }

            await accessRepository.AddAccessAsync(new UserServerAccess
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ServerId = existingServer.Id,
                AccessLevel = AccessLevel.Viewer,
                AddedAt = DateTime.UtcNow
            });

            logger.LogInformation("User {UserId} added access to existing server {ServerId}", userId, LogSanitizer.Sanitize(serverInfo.ServerId));

            return new ServerResponse
            {
                Id = existingServer.Id,
                ServerId = existingServer.ServerId,
                ServerKey = encryptionService.Decrypt(existingServer.ServerKey),
                Hostname = existingServer.Hostname,
                OperatingSystem = existingServer.OperatingSystem,
                AccessLevel = AccessLevel.Viewer,
                AddedAt = DateTime.UtcNow,
                LastSeen = existingServer.LastSeen,
                IsActive = existingServer.IsActive
            };
        }

        // For new servers, add both source and identifiers
        var sourceResponse = await goApiClient.AddServerSourceByKeyAsync(serverKey, "Web");
        if (sourceResponse == null)
        {
            logger.LogWarning("Failed to add Web source to Go API for server key ***");
        }
        else
        {
            logger.LogInformation("Successfully added Web source to Go API for server {ServerId}", LogSanitizer.Sanitize(sourceResponse.ServerId));

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

            var identifiersResponse = await goApiClient.AddServerSourceIdentifiersByKeyAsync(serverKey, identifiersRequest);
            if (identifiersResponse == null)
            {
                logger.LogWarning("Failed to add user identifier to Go API for server key {ServerKey}", LogSanitizer.MaskServerKey(serverKey));
            }
            else
            {
                logger.LogInformation("Successfully added user identifier to Go API for server {ServerId}", LogSanitizer.Sanitize(identifiersResponse.ServerId));
            }
        }

        var encryptedKey = encryptionService.Encrypt(serverKey ?? string.Empty);

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

        await serverRepository.AddAsync(newServer);

        await accessRepository.AddAccessAsync(new UserServerAccess
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServerId = newServer.Id,
            AccessLevel = AccessLevel.Owner,
            AddedAt = DateTime.UtcNow
        });

        logger.LogInformation("User {UserId} added new server {ServerId} as owner", userId, LogSanitizer.Sanitize(serverInfo.ServerId));

        return new ServerResponse
        {
            Id = newServer.Id,
            ServerId = newServer.ServerId,
            ServerKey = serverKey ?? string.Empty, // Return original key for new servers
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
        logger.LogInformation("User {UserId} attempting to remove server {ServerId}", userId, LogSanitizer.Sanitize(serverId));

        var hasAccess = await accessRepository.HasAccessAsync(userId, serverId ?? string.Empty);
        if (!hasAccess)
        {
            logger.LogWarning("User {UserId} attempted to remove server {ServerId} without access", userId, LogSanitizer.Sanitize(serverId));
            throw new UnauthorizedAccessException("You don't have access to this server");
        }

        // Get server to find server key
        var server = await serverRepository.GetByServerIdAsync(serverId ?? string.Empty);
        if (server == null)
        {
            logger.LogWarning("Server {ServerId} not found, removing access from DB only", LogSanitizer.Sanitize(serverId));
            await accessRepository.RemoveAccessAsync(userId, serverId ?? string.Empty);
            return;
        }

        // Decrypt server key for Go API call
        string decryptedKey;
        try
        {
            decryptedKey = string.IsNullOrEmpty(server.ServerKey)
                ? string.Empty
                : encryptionService.Decrypt(server.ServerKey);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            logger.LogWarning("ServerKey for server {ServerId} could not be decrypted - possibly encrypted with old key", LogSanitizer.Sanitize(server.ServerId));
            decryptedKey = string.Empty;
        }

        if (string.IsNullOrEmpty(decryptedKey))
        {
            logger.LogWarning("Decrypted server key is empty for server {ServerId}, removing access from DB only", LogSanitizer.Sanitize(serverId));
            await accessRepository.RemoveAccessAsync(userId, serverId ?? string.Empty);
            return;
        }

        // Try to remove user's identifier from Web source in Go API
        // This is the enterprise-level approach from Telegram bot
        try
        {
            logger.LogInformation("Removing user {UserId} identifier from Web source of server {ServerKey}", userId, LogSanitizer.MaskServerKey(decryptedKey));

            var request = new GoApiDeleteSourceIdentifiersRequest
            {
                Identifiers = new List<string> { userId.ToString() }
            };

            var result = await goApiClient.DeleteServerSourceIdentifiersByTypeAsync(decryptedKey, "Web", request);

            if (result != null)
            {
                logger.LogInformation("Successfully removed user identifier from Go API for server {ServerKey}", LogSanitizer.MaskServerKey(decryptedKey));
            }
            else
            {
                logger.LogWarning("Go API returned null when removing identifier, continuing with DB removal");
            }
        }
        catch (Exception ex)
        {
            // Graceful degradation - if API fails, still remove from DB
            logger.LogError(ex, "Failed to remove identifier from Go API for server {ServerKey}, continuing with DB removal", LogSanitizer.MaskServerKey(decryptedKey));
        }

        // Always remove access from DB (even if API call failed)
        await accessRepository.RemoveAccessAsync(userId, serverId ?? string.Empty);

        logger.LogInformation("User {UserId} removed access to server {ServerId}", userId, LogSanitizer.Sanitize(serverId));
    }

    public async Task ShareServerAsync(Guid ownerId, string serverId, string targetUserEmail, AccessLevel level)
    {
        logger.LogInformation("User {OwnerId} attempting to share server {ServerId} with {TargetEmail} at level {AccessLevel}", ownerId, LogSanitizer.Sanitize(serverId), LogSanitizer.MaskEmail(targetUserEmail), level);

        var ownerAccessLevel = await accessRepository.GetAccessLevelAsync(ownerId, serverId ?? string.Empty);
        if (ownerAccessLevel != AccessLevel.Owner)
        {
            logger.LogWarning("User {OwnerId} attempted to share server {ServerId} without owner rights. Current level: {AccessLevel}", ownerId, LogSanitizer.Sanitize(serverId), ownerAccessLevel);
            throw new UnauthorizedAccessException("Only owner can share server");
        }

        var targetUser = await userRepository.GetByEmailAsync(targetUserEmail ?? string.Empty) ?? throw new InvalidOperationException("Target user not found");

        var server = await serverRepository.GetByServerIdAsync(serverId ?? string.Empty) ?? throw new InvalidOperationException("Server not found");

        var targetHasAccess = await accessRepository.HasAccessAsync(targetUser.Id, serverId ?? string.Empty);
        if (targetHasAccess)
        {
            await accessRepository.UpdateAccessLevelAsync(targetUser.Id, serverId ?? string.Empty, level);
            logger.LogInformation("Updated access level for user {UserId} to server {ServerId}", targetUser.Id, LogSanitizer.Sanitize(serverId));
        }
        else
        {
            await accessRepository.AddAccessAsync(new UserServerAccess
            {
                Id = Guid.NewGuid(),
                UserId = targetUser.Id,
                ServerId = server.Id,
                AccessLevel = level,
                AddedAt = DateTime.UtcNow
            });

            logger.LogInformation("User {UserId} shared server {ServerId} with {TargetUserEmail} at level {AccessLevel}", ownerId, LogSanitizer.Sanitize(serverId), LogSanitizer.MaskEmail(targetUserEmail), level);
        }
    }

    private async Task<long?> GetUserTelegramIdAsync(Guid userId)
    {
        logger.LogInformation("Getting telegram_id for user {UserId}", userId);

        var telegramLogin = await externalLoginRepository.GetByUserIdAndProviderAsync(userId, OAuthProvider.Telegram);

        if (telegramLogin == null)
        {
            logger.LogWarning("No Telegram OAuth found for user {UserId}", userId);
            return null;
        }

        logger.LogInformation("Found Telegram OAuth for user {UserId}: ProviderUserId = {ProviderUserId}", userId, telegramLogin.ProviderUserId);

        if (long.TryParse(telegramLogin.ProviderUserId, out var telegramId))
        {
            logger.LogInformation("Successfully parsed telegram_id {TelegramId} for user {UserId}", telegramId, userId);
            return telegramId;
        }

        logger.LogWarning("Failed to parse telegram_id for user {UserId}: {ProviderUserId}", userId, telegramLogin.ProviderUserId);
        return null;
    }
}
