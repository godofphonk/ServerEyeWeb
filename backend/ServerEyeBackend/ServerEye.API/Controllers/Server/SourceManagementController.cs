namespace ServerEye.API.Controllers.Server;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Interfaces.Services;

[ApiController]
[Route("api/servers")]
[Authorize]
public class SourceManagementController : ControllerBase
{
    private readonly ISourceManagementService sourceManagementService;
    private readonly ILogger<SourceManagementController> logger;
    private readonly IGoApiClient goApiClient;
    private readonly IServerAccessService serverAccessService;

    public SourceManagementController(
        ISourceManagementService sourceManagementService,
        ILogger<SourceManagementController> logger,
        IGoApiClient goApiClient,
        IServerAccessService serverAccessService)
    {
        this.sourceManagementService = sourceManagementService;
        this.logger = logger;
        this.goApiClient = goApiClient;
        this.serverAccessService = serverAccessService;
    }

    /// <summary>
    /// Gets server sources and identifiers by server key.
    /// </summary>
    /// <param name="serverKey">The server key.</param>
    /// <returns>Server sources and identifiers.</returns>
    [HttpGet("by-key/{serverKey}/sources/identifiers")]
    public async Task<ActionResult<GoApiSourceIdentifiersResponse>> GetServerSourcesAndIdentifiers(
        string serverKey)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            logger.LogInformation(
                "Get sources and identifiers request - ServerKey: {ServerKey}, UserId: {UserId}",
                serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                userId);

            // Validate server key and get server info
            var serverInfo = await goApiClient.ValidateServerKeyAsync(serverKey ?? string.Empty);
            if (serverInfo == null)
            {
                logger.LogWarning("Server key validation failed for {ServerKey}", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
                return NotFound(new { message = "Server not found" });
            }

            // Check user access to server
            var hasAccess = await serverAccessService.HasAccessAsync(userId, serverInfo.ServerId);
            if (!hasAccess)
            {
                logger.LogWarning("User {UserId} does not have access to server {ServerId}", userId, serverInfo.ServerId?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
                return Forbid();
            }

            // Get sources and identifiers from Go API
            var result = await goApiClient.GetServerSourceIdentifiersByKeyAsync(serverKey ?? string.Empty);

            if (result == null)
            {
                logger.LogWarning("Failed to get sources and identifiers from Go API for server {ServerKey}", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
                return StatusCode(503, new { message = "Go API service unavailable" });
            }

            logger.LogInformation(
                "Successfully retrieved sources and identifiers for server {ServerKey}, Sources: {Sources}, IdentifierCount: {Count}",
                serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                string.Join(", ", result.Sources ?? []),
                result.Identifiers?.Sum(kvp => kvp.Value?.Count ?? 0) ?? 0);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting sources and identifiers for server {ServerKey}", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a source from a server by server key.
    /// </summary>
    /// <param name="serverKey">The server key.</param>
    /// <param name="source">The source to delete (e.g., "Web", "TGBot", "Telegram").</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("by-key/{serverKey}/sources/{source}")]
    public async Task<ActionResult<DeleteSourceResponseDto>> DeleteServerSource(
        string serverKey,
        string source)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            logger.LogInformation(
                "Delete source request - ServerKey: {ServerKey}, Source: {Source}, UserId: {UserId}",
                serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                source?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                userId);

            var result = await sourceManagementService.DeleteServerSourceAsync(userId, serverKey ?? string.Empty, source ?? string.Empty);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting source {Source} from server {ServerKey}", source?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes specific identifiers from a server by server key.
    /// </summary>
    /// <param name="serverKey">The server key.</param>
    /// <param name="request">The identifiers to delete.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("by-key/{serverKey}/sources/identifiers")]
    public async Task<ActionResult<DeleteSourceIdentifiersResponseDto>> DeleteServerSourceIdentifiers(
        string serverKey,
        [FromBody] DeleteSourceIdentifiersRequestDto request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            logger.LogInformation(
                "Delete identifiers request - ServerKey: {ServerKey}, IdentifierCount: {Count}, UserId: {UserId}",
                serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                request.Identifiers.Count,
                userId);

            var result = await sourceManagementService.DeleteServerSourceIdentifiersAsync(userId, serverKey ?? string.Empty, request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting identifiers from server {ServerKey}", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes specific identifiers from a server by server key and source type.
    /// </summary>
    /// <param name="serverKey">The server key.</param>
    /// <param name="sourceType">The source type (e.g., "Web", "TGBot", "Telegram").</param>
    /// <param name="request">The identifiers to delete.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("by-key/{serverKey}/sources/{sourceType}/identifiers")]
    public async Task<ActionResult<DeleteSourceIdentifiersResponseDto>> DeleteServerSourceIdentifiersByType(
        string serverKey,
        string sourceType,
        [FromBody] DeleteSourceIdentifiersRequestDto request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            logger.LogInformation(
                "Delete identifiers by type request - ServerKey: {ServerKey}, SourceType: {SourceType}, IdentifierCount: {Count}, UserId: {UserId}",
                serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                sourceType?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                request.Identifiers.Count,
                userId);

            var result = await sourceManagementService.DeleteServerSourceIdentifiersByTypeAsync(userId, serverKey ?? string.Empty, sourceType ?? string.Empty, request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting identifiers of type {SourceType} from server {ServerKey}", sourceType?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private Guid GetUserIdFromToken()
    {
        // This should be implemented based on your JWT token handling
        // For now, returning a placeholder - you'll need to implement actual token parsing
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
        }
        return Guid.Empty;
    }
}
