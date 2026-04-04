namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.Interfaces.Services;

[ApiController]
[Route("api/servers/by-key/{serverKey}")]
[Authorize]
public class StaticInfoController : ControllerBase
{
    private readonly IStaticInfoService staticInfoService;
    private readonly ILogger<StaticInfoController> logger;

    public StaticInfoController(
        IStaticInfoService staticInfoService,
        ILogger<StaticInfoController> logger)
    {
        this.staticInfoService = staticInfoService;
        this.logger = logger;
    }

    [HttpGet("static-info")]
    public async Task<ActionResult<GoApiStaticInfo>> GetStaticInfo(string serverKey)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user identifier");
            }

            var staticInfo = await this.staticInfoService.GetStaticInfoAsync(userGuid, serverKey);

            if (staticInfo is null)
            {
                return NotFound("Static information not found");
            }

            return Ok(staticInfo);
        }
        catch (UnauthorizedAccessException ex)
        {
            this.logger.LogWarning("Unauthorized access attempt for server key {ServerKey}: {Message}", serverKey, ex.Message);
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogError("Error retrieving static info for server key {ServerKey}: {Message}", serverKey, ex.Message);
            return StatusCode(500, ex.Message);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error retrieving static info for server key {ServerKey}", serverKey);
            return StatusCode(500, "Internal server error");
        }
    }
}
