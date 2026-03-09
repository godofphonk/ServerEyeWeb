namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using System.Security.Claims;

[ApiController]
[Route("api/servers/discovery")]
[Authorize]
public class ServerDiscoveryController(
    IServerDiscoveryService discoveryService,
    IUserExternalLoginRepository externalLoginRepository,
    ILogger<ServerDiscoveryController> logger) : ControllerBase
{
    [HttpGet("telegram")]
    public async Task<ActionResult<DiscoveredServersResponseDto>> FindServersByTelegram()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var telegramLogin = await externalLoginRepository.GetByUserIdAndProviderAsync(userId, OAuthProvider.Telegram);
            if (telegramLogin == null)
            {
                logger.LogWarning("User {UserId} does not have Telegram OAuth linked", userId);
                return BadRequest(new { message = "Telegram account not linked" });
            }

            if (!long.TryParse(telegramLogin.ProviderUserId, out var telegramId))
            {
                logger.LogError("Invalid telegram_id format for user {UserId}: {ProviderUserId}", userId, telegramLogin.ProviderUserId);
                return BadRequest(new { message = "Invalid Telegram ID format" });
            }

            logger.LogInformation("Finding servers for user {UserId} with telegram_id {TelegramId}", userId, telegramId);

            var result = await discoveryService.FindServersByTelegramIdAsync(userId, telegramId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding servers by Telegram");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportServersResponseDto>> ImportServers([FromBody] ImportServersRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            if (request.ServerIds == null || request.ServerIds.Count == 0)
            {
                return BadRequest(new { message = "No servers specified for import" });
            }

            logger.LogInformation("Importing {Count} servers for user {UserId}", request.ServerIds.Count, userId);

            var result = await discoveryService.ImportDiscoveredServersAsync(userId, request.ServerIds);

            if (result.ImportedCount == 0 && result.FailedCount > 0)
            {
                return BadRequest(new
                {
                    message = "Failed to import any servers",
                    errors = result.Errors
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing servers");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
