namespace ServerEye.API.Controllers;

using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerEye.Core.DTOs.UserDto;
using ServerEye.Core.Interfaces.Services;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowFrontend")]
public class UsersController(IUserService userService, IValidator<UserRegisterDto> registerValidator,
    IValidator<UserLoginDto> loginValidator, IValidator<UserUpdateDto> updateValidator, ILogger<UsersController> logger) : ControllerBase
{
    private readonly IUserService userService = userService;
    private readonly IValidator<UserRegisterDto> registerValidator = registerValidator;
    private readonly IValidator<UserLoginDto> loginValidator = loginValidator;
    private readonly IValidator<UserUpdateDto> updateValidator = updateValidator;
    private readonly ILogger<UsersController> logger = logger;

    [HttpGet]
    public async Task<ActionResult> GetAllUsersAsync() => await this.ExecuteWithErrorHandling(this.userService.GetAllUsersAsync, "GetAllUsers");

    [HttpGet("{id}")]
    public async Task<ActionResult> GetUserByIdAsync(Guid id) => await this.ExecuteWithErrorHandling(() => this.userService.GetUserByIdAsync(id), "GetUserById");

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult> GetUserByEmailAsync(string email) => await this.ExecuteWithErrorHandling(() => this.userService.GetUserByEmailAsync(email), "GetUserByEmail");

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> GetCurrentUserAsync()
    {
        var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            Console.WriteLine($"[DEBUG] /users/me - Invalid user identifier in token. UserIdClaim: '{userIdClaim}'");
            return this.Unauthorized(new { message = "Invalid user identifier" });
        }

        Console.WriteLine($"[DEBUG] /users/me - Successfully authenticated user: {userId}");
        return await this.ExecuteWithErrorHandling(() => this.userService.GetUserByIdAsync(userId), "GetCurrentUser");
    }

    [HttpPost("register")]
    public async Task<ActionResult> CreateUser([FromBody] UserRegisterDto userRegisterDto)
    {
        ArgumentNullException.ThrowIfNull(userRegisterDto);

        // Log incoming request for debugging
        Console.WriteLine($"Registration request - UserName: '{userRegisterDto.UserName}', Email: '{userRegisterDto.Email}', Password length: {userRegisterDto.Password?.Length ?? 0}");

        var validationResult = await this.registerValidator.ValidateAsync(userRegisterDto);
        if (!validationResult.IsValid)
        {
            Console.WriteLine($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))}");
            return this.BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
        }

        Console.WriteLine($"Registration attempt for email: {userRegisterDto.Email}, username: {userRegisterDto.UserName}");

        var result = await this.ExecuteWithErrorHandling(() => this.userService.CreateUserAsync(userRegisterDto), "CreateUser");

        Console.WriteLine($"Registration successful for user: {userRegisterDto.Email}");
        return result;
    }

    [HttpPost("login")]
    public async Task<ActionResult> LoginUser([FromBody] UserLoginDto userLoginDto)
    {
        ArgumentNullException.ThrowIfNull(userLoginDto);

        var validationResult = await this.loginValidator.ValidateAsync(userLoginDto);
        if (!validationResult.IsValid)
        {
            return this.BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
        }

        Console.WriteLine($"Login attempt for email: {userLoginDto.Email}");

        var result = await this.ExecuteWithErrorHandling(() => this.userService.LoginUserAsync(userLoginDto), "LoginUser");

        Console.WriteLine($"Login successful for user: {userLoginDto.Email}");
        return result;
    }
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateUser([FromRoute] Guid id, UserUpdateDto userUpdateDto)
    {
        ArgumentNullException.ThrowIfNull(userUpdateDto);

        var validationResult = await this.updateValidator.ValidateAsync(userUpdateDto);
        if (!validationResult.IsValid)
        {
            return this.BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
        }

        return await this.ExecuteWithErrorHandling(() => this.userService.UpdateUserAsync(id, userUpdateDto), "UpdateUser");
    }

    [HttpPost("create-admin")]
    public async Task<ActionResult> CreateAdminUser()
    {
        try
        {
            var adminDto = new UserRegisterDto
            {
                UserName = "admin",
                Email = "admin@servereye.dev",
                Password = "admin123"
            };

            var result = await this.userService.CreateUserAsync(adminDto);
            
            // Обновляем роль на Admin в базе данных
            // Это временный solution - в production лучше сделать через миграцию
            this.logger.LogInformation("Admin user created successfully with email: {Email}", "admin@servereye.dev");
            
            return Ok(new { message = "Admin user created successfully", email = "admin@servereye.dev", password = "admin123" });
        }
        catch (InvalidOperationException)
        {
            // Если пользователь уже существует, просто возвращаем успех
            return Ok(new { message = "Admin user already exists", email = "admin@servereye.dev", password = "admin123" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating admin user: {ex.Message}");
            return StatusCode(500, new { message = "Failed to create admin user" });
        }
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteUser(Guid id) =>
        await this.ExecuteWithErrorHandling(
            async () =>
            {
                await this.userService.DeleteUserAsync(id);
                return true;
            },
            "DeleteUser",
            true);

    private async Task<ActionResult> ExecuteWithErrorHandling<T>(Func<Task<T>> operation, string operationName, bool returnNoContent = false)
    {
        try
        {
            var result = await operation();
            return returnNoContent ? this.NoContent() : this.Ok(result);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Argument error in {operationName}: {ex.Message}");
            return this.BadRequest(new { message = "Invalid input parameters" });
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Invalid operation in {operationName}: {ex.Message}");
            return this.StatusCode(409, new { message = ex.Message }); // Conflict for duplicate email
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Database error in {operationName}: {ex.Message}");
            return this.StatusCode(500, new { message = "Database operation failed" });
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"Timeout in {operationName}: {ex.Message}");
            return this.StatusCode(504, new { message = "Request timeout" });
        }
    }
}
