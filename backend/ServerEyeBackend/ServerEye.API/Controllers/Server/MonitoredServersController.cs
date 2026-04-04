namespace ServerEye.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MonitoredServersController : ControllerBase
{
    private readonly IServerAccessService serverAccessService;
    private readonly ILogger<MonitoredServersController> logger;

    public MonitoredServersController(IServerAccessService serverAccessService, ILogger<MonitoredServersController> logger)
    {
        this.serverAccessService = serverAccessService;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServerResponse>>> GetUserServers()
    {
        try
        {
            var userId = this.GetUserId();
            var servers = await this.serverAccessService.GetUserServersAsync(userId);
            return this.Ok(servers);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting user servers");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("add")]
    public async Task<ActionResult<ServerResponse>> AddServer([FromBody] AddServerRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            var server = await this.serverAccessService.AddServerAsync(userId, request.ServerKey);
            return this.Ok(server);
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning(ex, "Invalid operation while adding server");
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error adding server");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{serverId}")]
    public async Task<IActionResult> RemoveServer(string serverId)
    {
        try
        {
            var userId = this.GetUserId();

            // Check if it's a UUID (new format) or serverId (old format)
            if (Guid.TryParse(serverId, out var serverGuid))
            {
                // It's a UUID - need to find the server by UUID first
                var servers = await this.serverAccessService.GetUserServersAsync(userId);
                var server = servers.FirstOrDefault(s => s.Id.ToString() == serverId);
                if (server == null)
                {
                    return NotFound("Server not found");
                }
                await this.serverAccessService.RemoveServerAsync(userId, server.ServerId);
            }
            else
            {
                // It's a serverId string
                await this.serverAccessService.RemoveServerAsync(userId, serverId);
            }

            return this.NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            this.logger.LogWarning(ex, "Unauthorized access while removing server");
            return this.Forbid();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error removing server");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{serverId}/share")]
    public async Task<IActionResult> ShareServer(string serverId, [FromBody] ShareServerRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            await this.serverAccessService.ShareServerAsync(userId, serverId, request.TargetUserEmail, request.AccessLevel);
            return this.Ok(new { message = "Server shared successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            this.logger.LogWarning(ex, "Unauthorized access while sharing server");
            return this.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning(ex, "Invalid operation while sharing server");
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error sharing server");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }

        return userId;
    }
}
