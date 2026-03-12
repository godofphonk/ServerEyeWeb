namespace ServerEye.Core.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class ServerDiscoveryService(
    IGoApiClient goApiClient,
    IMonitoredServerRepository serverRepository,
    IUserServerAccessRepository accessRepository,
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

                await accessRepository.AddAccessAsync(new UserServerAccess
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ServerId = existingServer.Id,
                    AccessLevel = AccessLevel.Viewer,
                    AddedAt = DateTime.UtcNow
                });

                var decryptedKey = string.IsNullOrEmpty(existingServer.ServerKey)
                    ? string.Empty
                    : encryptionService.Decrypt(existingServer.ServerKey);

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

                logger.LogInformation("Successfully imported server {ServerId} for user {UserId}", serverId, userId);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to import server {serverId}: {ex.Message}";
                errors.Add(errorMessage);
                logger.LogError(ex, "Error importing server {ServerId} for user {UserId}", serverId, userId);
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
}
