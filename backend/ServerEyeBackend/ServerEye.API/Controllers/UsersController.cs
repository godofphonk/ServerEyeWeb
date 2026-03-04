namespace ServerEye.API.Controllers;

using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.DTOs.UserDto;
using ServerEye.Core.Interfaces.Services;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowFrontend")]
public class UsersController(IUserService userService, IAuthService authService, IValidator<UserRegisterDto> registerValidator,
    IValidator<UserLoginDto> loginValidator, IValidator<UserUpdateDto> updateValidator, ILogger<UsersController> logger) : ControllerBase
{
    private readonly IUserService userService = userService;
    private readonly IAuthService authService = authService;
    private readonly IValidator<UserRegisterDto> registerValidator = registerValidator;
    private readonly IValidator<UserLoginDto> loginValidator = loginValidator;
    private readonly IValidator<UserUpdateDto> updateValidator = updateValidator;
    private readonly ILogger<UsersController> logger = logger;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> GetAllUsersAsync() => await this.ExecuteWithErrorHandling(this.userService.GetAllUsersAsync, "GetAllUsers");

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> GetCurrentUser()
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            var user = await this.userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return this.NotFound(new { message = "User not found" });
            }

            return this.Ok(user);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting current user");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetUserByIdAsync(Guid id) => await this.ExecuteWithErrorHandling(() => this.userService.GetUserByIdAsync(id), "GetUserById");

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult> GetUserByEmailAsync(string email) => await this.ExecuteWithErrorHandling(() => this.userService.GetUserByEmailAsync(email), "GetUserByEmail");

    [HttpPost("register")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<ActionResult> CreateUser([FromBody] UserRegisterDto userRegisterDto)
    {
        ArgumentNullException.ThrowIfNull(userRegisterDto);

        // Log incoming request for debugging
        this.logger.LogInformation(
            "Registration request - UserName: {UserName}, Email: {Email}, Password length: {PasswordLength}",
            userRegisterDto.UserName,
            userRegisterDto.Email,
            userRegisterDto.Password?.Length ?? 0);

        var validationResult = await this.registerValidator.ValidateAsync(userRegisterDto);
        if (!validationResult.IsValid)
        {
            this.logger.LogWarning("Validation failed: {Errors}", string.Join(", ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return this.BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
        }

        this.logger.LogInformation("Registration attempt for email: {Email}, username: {UserName}", userRegisterDto.Email, userRegisterDto.UserName);

        var result = await this.ExecuteWithErrorHandling(() => this.userService.CreateUserAsync(userRegisterDto), "CreateUser");

        this.logger.LogInformation("Registration successful for user: {Email}", userRegisterDto.Email);
        return result;
    }

    [HttpPost("login")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
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

    [HttpPost("verify-email")]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var user = await this.userService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return this.BadRequest(new { message = "User not found" });
            }

            var result = await this.authService.VerifyEmailAsync(user.Id, request.Code);
            if (!result)
            {
                return this.BadRequest(new { message = "Invalid or expired verification code" });
            }

            Console.WriteLine($"Email verified for user: {request.Email}");
            return this.Ok(new { message = "Email verified successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying email: {ex.Message}");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
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
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"Authentication failed in {operationName}: {ex.Message}");
            return this.StatusCode(401, new { message = "Invalid email or password" });
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Access denied in {operationName}: {ex.Message}");
            return this.StatusCode(401, new { message = ex.Message });
        }
    }
}
