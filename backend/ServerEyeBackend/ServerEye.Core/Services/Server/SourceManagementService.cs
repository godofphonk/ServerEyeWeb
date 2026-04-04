namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Helpers;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class SourceManagementService(
    IGoApiClient goApiClient,
    IMonitoredServerRepository serverRepository,
    IUserServerAccessRepository accessRepository,
    ILogger<SourceManagementService> logger) : ISourceManagementService
{
    public async Task<DeleteSourceResponseDto> DeleteServerSourceAsync(Guid userId, string serverKey, string source)
    {
        logger.LogInformation("Deleting source {Source} from server {ServerKey} for user {UserId}", LogSanitizer.Sanitize(source), LogSanitizer.MaskServerKey(serverKey), userId);

        try
        {
            // Validate server key and get server info
            var serverInfo = await goApiClient.ValidateServerKeyAsync(serverKey);
            if (serverInfo == null)
            {
                logger.LogWarning("Invalid server key {ServerKey}", LogSanitizer.MaskServerKey(serverKey));
                return new DeleteSourceResponseDto
                {
                    Message = "Invalid server key",
                    ServerId = string.Empty,
                    Source = source,
                    Success = false
                };
            }

            // Verify user has access to the server
            var server = await serverRepository.GetByServerIdAsync(serverInfo.ServerId);
            if (server == null)
            {
                logger.LogWarning("Server not found for ID {ServerId}", serverInfo.ServerId);
                return new DeleteSourceResponseDto
                {
                    Message = "Server not found",
                    ServerId = serverInfo.ServerId,
                    Source = source,
                    Success = false
                };
            }

            var hasAccess = await accessRepository.HasAccessAsync(userId, server.ServerId);
            if (!hasAccess)
            {
                logger.LogWarning("User {UserId} does not have access to server {ServerId}", userId, server.ServerId);
                return new DeleteSourceResponseDto
                {
                    Message = "Access denied",
                    ServerId = server.ServerId,
                    Source = source,
                    Success = false
                };
            }

            // Delete source from Go API
            var response = await goApiClient.DeleteServerSourceByKeyAsync(serverKey, source);
            if (response == null)
            {
                logger.LogWarning("Failed to delete source {Source} from Go API for server {ServerKey}", source?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
                return new DeleteSourceResponseDto
                {
                    Message = "Failed to delete source from monitoring service",
                    ServerId = server.ServerId,
                    Source = source ?? string.Empty,
                    Success = false
                };
            }

            logger.LogInformation("Successfully deleted source {Source} from server {ServerKey} for user {UserId}", source?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", userId);
            return new DeleteSourceResponseDto
            {
                Message = response.Message,
                ServerId = response.ServerId,
                Source = response.Source,
                DeletedIdentifiers = response.DeletedIdentifiers,
                Success = true
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting source {Source} from server {ServerKey} for user {UserId}", LogSanitizer.Sanitize(source), LogSanitizer.MaskServerKey(serverKey), userId);
            return new DeleteSourceResponseDto
            {
                Message = "Internal server error",
                ServerId = string.Empty,
                Source = source,
                Success = false
            };
        }
    }

    public async Task<DeleteSourceIdentifiersResponseDto> DeleteServerSourceIdentifiersAsync(Guid userId, string serverKey, DeleteSourceIdentifiersRequestDto request)
    {
        logger.LogInformation("Deleting identifiers from server {ServerKey} for user {UserId}", LogSanitizer.MaskServerKey(serverKey), userId);

        try
        {
            // Validate server key and get server info
            var serverInfo = await goApiClient.ValidateServerKeyAsync(serverKey);
            if (serverInfo == null)
            {
                logger.LogWarning("Invalid server key {ServerKey}", LogSanitizer.MaskServerKey(serverKey));
                return new DeleteSourceIdentifiersResponseDto
                {
                    Message = "Invalid server key",
                    ServerId = string.Empty,
                    SourceType = string.Empty,
                    Success = false
                };
            }

            // Verify user has access to the server
            var server = await serverRepository.GetByServerIdAsync(serverInfo.ServerId);
            if (server == null)
            {
                logger.LogWarning("Server not found for ID {ServerId}", serverInfo.ServerId);
                return new DeleteSourceIdentifiersResponseDto
                {
                    Message = "Server not found",
                    ServerId = serverInfo.ServerId,
                    SourceType = string.Empty,
                    Success = false
                };
            }

            var hasAccess = await accessRepository.HasAccessAsync(userId, server.ServerId);
            if (!hasAccess)
            {
                logger.LogWarning("User {UserId} does not have access to server {ServerId}", userId, server.ServerId);
                return new DeleteSourceIdentifiersResponseDto
                {
                    Message = "Access denied",
                    ServerId = server.ServerId,
                    SourceType = string.Empty,
                    Success = false
                };
            }

            // Delete identifiers from Go API
            var goApiRequest = new GoApiDeleteSourceIdentifiersRequest
            {
                Identifiers = request.Identifiers
            };

            var response = await goApiClient.DeleteServerSourceIdentifiersByKeyAsync(serverKey, goApiRequest);
            if (response == null)
            {
                logger.LogWarning("Failed to delete identifiers from Go API for server {ServerKey}", LogSanitizer.MaskServerKey(serverKey));
                return new DeleteSourceIdentifiersResponseDto
                {
                    Message = "Failed to delete identifiers from monitoring service",
                    ServerId = server.ServerId,
                    SourceType = string.Empty,
                    Success = false
                };
            }

            logger.LogInformation("Successfully deleted identifiers from server {ServerKey} for user {UserId}", LogSanitizer.MaskServerKey(serverKey), userId);
            return new DeleteSourceIdentifiersResponseDto
            {
                Message = response.Message,
                ServerId = response.ServerId,
                SourceType = response.Source,
                DeletedIdentifiers = response.DeletedIdentifiers,
                Success = true
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting identifiers from server {ServerKey} for user {UserId}", LogSanitizer.MaskServerKey(serverKey), userId);
            return new DeleteSourceIdentifiersResponseDto
            {
                Message = "Internal server error",
                ServerId = string.Empty,
                SourceType = string.Empty,
                Success = false
            };
        }
    }

    public async Task<DeleteSourceIdentifiersResponseDto> DeleteServerSourceIdentifiersByTypeAsync(Guid userId, string serverKey, string sourceType, DeleteSourceIdentifiersRequestDto request)
    {
        logger.LogInformation("Deleting identifiers of type {SourceType} from server {ServerKey} for user {UserId}", LogSanitizer.Sanitize(sourceType), LogSanitizer.MaskServerKey(serverKey), userId);

        try
        {
            // Validate server key and get server info
            var serverInfo = await goApiClient.ValidateServerKeyAsync(serverKey);
            if (serverInfo == null)
            {
                logger.LogWarning("Invalid server key {ServerKey}", LogSanitizer.MaskServerKey(serverKey));
                return new DeleteSourceIdentifiersResponseDto
                {
                    Message = "Invalid server key",
                    ServerId = string.Empty,
                    SourceType = sourceType,
                    Success = false
                };
            }

            // Verify user has access to the server
            var server = await serverRepository.GetByServerIdAsync(serverInfo.ServerId);
            if (server == null)
            {
                logger.LogWarning("Server not found for ID {ServerId}", serverInfo.ServerId);
                return new DeleteSourceIdentifiersResponseDto
                {
                    Message = "Server not found",
                    ServerId = serverInfo.ServerId,
                    SourceType = sourceType,
                    Success = false
                };
            }

            var hasAccess = await accessRepository.HasAccessAsync(userId, server.ServerId);
            if (!hasAccess)
            {
                logger.LogWarning("User {UserId} does not have access to server {ServerId}", userId, server.ServerId);
                return new DeleteSourceIdentifiersResponseDto
                {
                    Message = "Access denied",
                    ServerId = server.ServerId,
                    SourceType = sourceType,
                    Success = false
                };
            }

            // Delete identifiers from Go API
            var goApiRequest = new GoApiDeleteSourceIdentifiersRequest
            {
                Identifiers = request.Identifiers
            };

            var response = await goApiClient.DeleteServerSourceIdentifiersByTypeAsync(serverKey, sourceType, goApiRequest);
            if (response == null)
            {
                logger.LogWarning("Failed to delete identifiers of type {SourceType} from Go API for server {ServerKey}", sourceType?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
                return new DeleteSourceIdentifiersResponseDto
                {
                    Message = "Failed to delete identifiers from monitoring service",
                    ServerId = server.ServerId,
                    SourceType = sourceType ?? string.Empty,
                    Success = false
                };
            }

            logger.LogInformation("Successfully deleted identifiers of type {SourceType} from server {ServerKey} for user {UserId}", sourceType?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", userId);
            return new DeleteSourceIdentifiersResponseDto
            {
                Message = response.Message,
                ServerId = response.ServerId,
                SourceType = response.Source,
                DeletedIdentifiers = response.DeletedIdentifiers,
                Success = true
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting identifiers of type {SourceType} from server {ServerKey} for user {UserId}", LogSanitizer.Sanitize(sourceType), LogSanitizer.MaskServerKey(serverKey), userId);
            return new DeleteSourceIdentifiersResponseDto
            {
                Message = "Internal server error",
                ServerId = string.Empty,
                SourceType = sourceType,
                Success = false
            };
        }
    }
}
