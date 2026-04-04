namespace ServerEye.Core.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class ServerDiscoveryService(
    IGoApiClient goApiClient,
    IMonitoredServerRepository serverRepository,
    IUserServerAccessRepository accessRepository,
    IUserExternalLoginRepository externalLoginRepository,
    IEncryptionService encryptionService,
    IConfiguration configuration,
    ILogger<ServerDiscoveryService> logger) : IServerDiscoveryService
{
    public async Task<DiscoveredServersResponseDto> FindServersByTelegramIdAsync(Guid userId, long telegramId)
    {
        logger.LogInformation("Finding servers for user {UserId} with telegram_id {TelegramId}", userId, telegramId);

        try
        {
            var servers = await goApiClient.FindServersByTelegramIdAsync(telegramId);

            if (servers == null || servers.Count == 0)
            {
                logger.LogInformation("No servers found for telegram_id {TelegramId}", telegramId);
                return new DiscoveredServersResponseDto
                {
                    TelegramId = telegramId,
                    Servers = new List<DiscoveredServerDto>(),
                    TotalCount = 0,
                    HasTelegramBot = true,
                    TelegramBotUsername = configuration["TelegramBot:Username"] ?? "ServerEyeBot"
                };
            }

            var discoveredServers = new List<DiscoveredServerDto>();

            foreach (var server in servers)
            {
                var existingServer = await serverRepository.GetByServerIdAsync(server.ServerId);
                var hasAccess = existingServer != null && await accessRepository.HasAccessAsync(userId, server.ServerId);

                discoveredServers.Add(new DiscoveredServerDto
                {
                    ServerId = server.ServerId,
                    Hostname = server.Hostname,
                    OperatingSystem = server.OperatingSystem,
                    LastSeen = server.LastSeen,
                    AgentVersion = server.AgentVersion,
                    AddedVia = "Telegram Bot",
                    CanImport = !hasAccess
                });
            }

            logger.LogInformation(
                "Found {TotalCount} servers for telegram_id {TelegramId}, {CanImportCount} can be imported",
                discoveredServers.Count,
                telegramId,
                discoveredServers.Count(s => s.CanImport));

            return new DiscoveredServersResponseDto
            {
                TelegramId = telegramId,
                Servers = discoveredServers,
                TotalCount = discoveredServers.Count,
                HasTelegramBot = true,
                TelegramBotUsername = configuration["TelegramBot:Username"] ?? "ServerEyeBot"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding servers for telegram_id {TelegramId}", telegramId);
            throw;
        }
    }

    public async Task<ImportServersResponseDto> ImportDiscoveredServersAsync(Guid userId, List<string> serverIds)
    {
        logger.LogInformation("Importing {Count} servers for user {UserId}", serverIds.Count, userId);

        var imported = new List<ServerResponse>();
        var errors = new List<string>();

        // Get telegram_id if user has Telegram OAuth linked
        var telegramId = await this.GetUserTelegramIdAsync(userId);

        foreach (var serverId in serverIds)
        {
            try
            {
                var existingServer = await serverRepository.GetByServerIdAsync(serverId);

                if (existingServer == null)
                {
                    errors.Add($"Server {serverId} not found in database");
                    logger.LogWarning("Server {ServerId} not found for import", serverId);
                    continue;
                }

                var hasAccess = await accessRepository.HasAccessAsync(userId, serverId);
                if (hasAccess)
                {
                    errors.Add($"Server {serverId} already added to your account");
                    logger.LogWarning("User {UserId} already has access to server {ServerId}", userId, serverId);
                    continue;
                }

                // Add Telegram source to Go API for discovered servers
                var decryptedKey = string.IsNullOrEmpty(existingServer.ServerKey)
                    ? string.Empty
                    : encryptionService.Decrypt(existingServer.ServerKey);

                if (!string.IsNullOrEmpty(decryptedKey))
                {
                    // Add Telegram source to Go API
                    var sourceResponse = await goApiClient.AddServerSourceByKeyAsync(decryptedKey, "Telegram");
                    if (sourceResponse == null)
                    {
                        logger.LogWarning("Failed to add Telegram source to Go API for server {ServerId}", serverId);

                        // Continue with import even if source add fails
                    }
                    else
                    {
                        logger.LogInformation("Successfully added Telegram source to Go API for server {ServerId}", serverId);

                        // Add user identifier to Telegram source
                        var identifiersRequest = new GoApiSourceIdentifiersRequest
                        {
                            SourceType = "Telegram",
                            Identifiers = new List<string> { userId.ToString() },
                            IdentifierType = "user_id",
                            TelegramId = telegramId,
                            Metadata = new Dictionary<string, object>
                            {
                                { "added_at", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                                { "source", "ServerEyeWeb" },
                                { "discovery_method", "TelegramBot" }
                            }
                        };

                        if (telegramId.HasValue)
                        {
                            identifiersRequest.Metadata!["telegram_linked_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        }

                        var identifiersResponse = await goApiClient.AddServerSourceIdentifiersByKeyAsync(decryptedKey, identifiersRequest);
                        if (identifiersResponse == null)
                        {
                            logger.LogWarning("Failed to add user identifier to Telegram source for server {ServerId}", serverId);
                        }
                        else
                        {
                            logger.LogInformation("Successfully added user identifier to Telegram source for server {ServerId}", serverId);
                        }
                    }
                }

                await accessRepository.AddAccessAsync(new UserServerAccess
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ServerId = existingServer.Id,
                    AccessLevel = AccessLevel.Viewer,
                    AddedAt = DateTime.UtcNow
                });

                imported.Add(new ServerResponse
                {
                    Id = existingServer.Id,
                    ServerId = existingServer.ServerId,
                    ServerKey = decryptedKey,
                    Hostname = existingServer.Hostname,
                    OperatingSystem = existingServer.OperatingSystem,
                    AccessLevel = AccessLevel.Viewer,
                    AddedAt = DateTime.UtcNow,
                    LastSeen = existingServer.LastSeen,
                    IsActive = existingServer.IsActive
                });

                logger.LogInformation("Successfully imported server {ServerId} for user {UserId}", serverId?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", userId);
            }
            catch (Exception ex)
            {
                errors.Add($"Server {serverId}: {ex.Message}");
                logger.LogError(ex, "Error importing server {ServerId} for user {UserId}", serverId?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", userId);
            }
        }

        logger.LogInformation(
            "Import completed for user {UserId}: {ImportedCount} imported, {FailedCount} failed",
            userId,
            imported.Count,
            errors.Count);

        return new ImportServersResponseDto
        {
            ImportedCount = imported.Count,
            FailedCount = errors.Count,
            Servers = imported,
            Errors = errors
        };
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
