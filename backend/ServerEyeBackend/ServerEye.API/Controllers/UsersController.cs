namespace ServerEye.API.Controllers;

using FluentValidation;
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
    IValidator<UserLoginDto> loginValidator, IValidator<UserUpdateDto> updateValidator, ILogger<UsersController> logger) : BaseApiController
{
    private readonly IUserService userService = userService;
    private readonly IAuthService authService = authService;
    private readonly IValidator<UserRegisterDto> registerValidator = registerValidator;
    private readonly IValidator<UserLoginDto> loginValidator = loginValidator;
    private readonly IValidator<UserUpdateDto> updateValidator = updateValidator;
    private readonly ILogger<UsersController> logger = logger;

    [HttpGet]
    public async Task<ActionResult<List<ServerEye.Core.DTOs.UserDto.UserData>>> GetAllUsersAsync()
    {
        return await ExecuteWithErrorHandling(userService.GetAllUsersAsync);
    }

    [HttpGet("me")]
    public async Task<ActionResult<ServerEye.Core.DTOs.UserDto.UserData>> GetCurrentUser()
    {
        return await ExecuteWithErrorHandling(async () => 
        {
            var userId = GetUserId();
            var user = await userService.GetUserByIdAsync(userId);
            return user ?? throw new KeyNotFoundException("User not found");
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServerEye.Core.DTOs.UserDto.UserData>> GetUserByIdAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () => await userService.GetUserByIdAsync(id) ?? throw new KeyNotFoundException("User not found"));
    }

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<ServerEye.Core.DTOs.UserDto.UserData>> GetUserByEmailAsync(string email)
    {
        return await ExecuteWithErrorHandling(async () => await userService.GetUserByEmailAsync(email) ?? throw new KeyNotFoundException("User not found"));
    }

    [HttpPost("register")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<ActionResult<ServerEye.Core.DTOs.Auth.AuthResponseDto>> CreateUser([FromBody] UserRegisterDto userRegisterDto)
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
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
        }

        this.logger.LogInformation("Registration attempt for email: {Email}, username: {UserName}", userRegisterDto.Email, userRegisterDto.UserName);

        var result = await ExecuteWithErrorHandling(() => userService.CreateUserAsync(userRegisterDto));

        this.logger.LogInformation("Registration successful for user: {Email}", userRegisterDto.Email);
        return result;
    }

    [HttpPost("login")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<ActionResult<ServerEye.Core.DTOs.Auth.AuthResponseDto>> LoginUser([FromBody] UserLoginDto userLoginDto)
    {
        ArgumentNullException.ThrowIfNull(userLoginDto);

        var validationResult = await this.loginValidator.ValidateAsync(userLoginDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
        }

        this.logger.LogInformation("Login attempt for email: {Email}", userLoginDto.Email);

        var result = await ExecuteWithErrorHandling(() => userService.LoginUserAsync(userLoginDto));

        this.logger.LogInformation("Login successful for user: {Email}", userLoginDto.Email);
        return result;
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<object>> VerifyEmail([FromBody] VerifyEmailDto request)
    {
        return await ExecuteWithErrorHandling(async () => 
        {
            ArgumentNullException.ThrowIfNull(request);

            var user = await userService.GetUserByEmailAsync(request.Email) ?? throw new ArgumentException("User not found");

            var result = await authService.VerifyEmailAsync(user.Id, request.Code);
            if (!result)
            {
                throw new InvalidOperationException("Invalid or expired verification code");
            }

            this.logger.LogInformation("Email verified for user: {Email}", request.Email);
            return new { message = "Email verified successfully" };
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServerEye.Core.DTOs.UserDto.UserData>> UpdateUser([FromRoute] Guid id, UserUpdateDto userUpdateDto)
    {
        ArgumentNullException.ThrowIfNull(userUpdateDto);

        var validationResult = await this.updateValidator.ValidateAsync(userUpdateDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
        }

        return await ExecuteWithErrorHandling(async () => await userService.UpdateUserAsync(id, userUpdateDto));
    }

    [HttpPost("create-admin")]
    public async Task<ActionResult<object>> CreateAdminUser()
    {
        return await ExecuteWithErrorHandling(async () => 
        {
            var adminDto = new UserRegisterDto
            {
                UserName = "admin",
                Email = "admin@servereye.dev",
                Password = "admin123"
            };

            var result = await userService.CreateUserAsync(adminDto);
            
            // Обновляем роль на Admin в базе данных
            // Это временный solution - в production лучше сделать через миграцию
            this.logger.LogInformation("Admin user created successfully with email: {Email}", "admin@servereye.dev");
            
            return new { message = "Admin user created successfully", email = "admin@servereye.dev", password = "admin123" };
        });
    }

    [HttpDelete]
    public async Task<ActionResult<bool>> DeleteUser(Guid id)
    {
        return await ExecuteWithErrorHandling(async () => 
        {
            await userService.DeleteUserAsync(id);
            return true;
        });
    }
}
