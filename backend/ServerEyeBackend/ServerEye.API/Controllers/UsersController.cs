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
    public async Task<ActionResult> GetAllUsersAsync() =>
        this.Ok(await this.userService.GetAllUsersAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult> GetUserByIdAsync(Guid id) =>
        this.Ok(await this.userService.GetUserByIdAsync(id));

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult> GetUserByEmailAsync(string email) =>
        this.Ok(await this.userService.GetUserByEmailAsync(email));

    [HttpPost("register")]
    public async Task<ActionResult> CreateUser([FromBody] UserRegisterDto userRegisterDto) =>
        this.Ok(await this.userService.CreateUserAsync(userRegisterDto));

    [HttpPost("login")]
    public async Task<ActionResult> LoginUser([FromBody] UserLoginDto userLoginDto) =>
        this.Ok(await this.userService.LoginUserAsync(userLoginDto));
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateUser([FromRoute] Guid id, UserUpdateDto userUpdateDto) =>
        this.Ok(await this.userService.UpdateUserAsync(id, userUpdateDto));

    [HttpDelete]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        await this.userService.DeleteUserAsync(id);
        return this.NoContent();
    }
}
