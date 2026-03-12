namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs;
using ServerEye.Core.Interfaces.Services;

[Route("api/[controller]")]
[EnableCors("AllowFrontend")]
[Authorize]
public class ServersController : BaseApiController
{
    private readonly IServersService serversService;
    private readonly ServersConfiguration configuration;

    public ServersController(IServersService serversService, ServersConfiguration configuration)
    {
        this.serversService = serversService;
        this.configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<ServersResponseDto>> GetServers()
    {
        if (configuration.UseMockData)
        {
            return Success(await serversService.GetMockServersAsync());
        }

        var userId = GetUserId();
        var servers = await serversService.GetUserServersAsync(userId);
        return Success(servers);
    }
}
