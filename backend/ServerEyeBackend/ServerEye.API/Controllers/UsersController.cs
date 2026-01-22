namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.UserDto;
using ServerEye.Core.Interfaces.Services;

[ApiController]
[Route("api/[controller]")]
#pragma warning disable CA1515
public class UsersController(IUserService userService) : ControllerBase
#pragma warning restore CA1515
{
    private readonly IUserService userService = userService;

    [HttpGet]
    public async Task<ActionResult> GetAllUsersAsync() => this.Ok(await this.userService.GetAllUsersAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult> GetUserByIdAsync(Guid id) => this.Ok(await this.userService.GetUserByIdAsync(id));

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult> GetUserByEmailAsync(string email) => this.Ok(await this.userService.GetUserByEmailAsync(email));

    [HttpPost]
    public async Task<ActionResult> CreateUser([FromBody] UserRequestDto userRequestDto) => this.Ok(await this.userService.CreateUserAsync(userRequestDto));

    [HttpPut]
    public async Task<ActionResult> UpdateUser([FromBody] UserRequestDto userRequestDto) => this.Ok(await this.userService.UpdateUserAsync(userRequestDto));

    [HttpDelete]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        await this.userService.DeleteUserAsync(id);
        return this.NoContent();
    }
}
