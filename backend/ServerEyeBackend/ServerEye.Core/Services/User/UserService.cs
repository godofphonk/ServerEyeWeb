namespace ServerEye.Core.Services;

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.DTOs.UserDto;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public sealed class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtService jwtService, IRefreshTokenRepository refreshTokenRepository, IAuthService authService, IConfiguration configuration, ILogger<UserService> logger) : IUserService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly IJwtService jwtService = jwtService;
    private readonly IRefreshTokenRepository refreshTokenRepository = refreshTokenRepository;
    private readonly IAuthService authService = authService;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger<UserService> logger = logger;

    public async Task<UserData?> GetUserByIdAsync(Guid id)
    {
        var user = await this.userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }
        return new UserData()
        {
            Email = user.Email ?? string.Empty,
            Id = user.Id,
            UserName = user.UserName,
            Role = user.Role.ToString().ToUpperInvariant(),
            ServerId = user.ServerId,
            IsEmailVerified = user.IsEmailVerified,
            EmailVerifiedAt = user.EmailVerifiedAt,
        };
    }

    public async Task<UserData?> GetUserByEmailAsync(string email)
    {
        var user = await this.userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return null;
        }
        return new UserData()
        {
            Email = user.Email ?? string.Empty,
            Id = user.Id,
            UserName = user.UserName,
            Role = user.Role.ToString().ToUpperInvariant(),
            ServerId = user.ServerId,
            IsEmailVerified = user.IsEmailVerified,
            EmailVerifiedAt = user.EmailVerifiedAt,
        };
    }

    public async Task<List<UserData>> GetAllUsersAsync()
    {
        var users = await this.userRepository.GetAllAsync();

        return users.Select(user => new UserData
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email ?? string.Empty,
            Role = user.Role.ToString().ToUpperInvariant(),
            ServerId = user.ServerId,
            IsEmailVerified = user.IsEmailVerified,
            EmailVerifiedAt = user.EmailVerifiedAt,
        }).ToList();
    }

    public async Task<AuthResponseDto> CreateUserAsync(UserRegisterDto userRegisterDto)
    {
        ArgumentNullException.ThrowIfNull(userRegisterDto);

        this.logger.LogInformation("Starting user registration for email: {Email}, username: {UserName}", userRegisterDto.Email?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", userRegisterDto.UserName?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

        // Check if user with this email already exists
        var existingUser = await this.userRepository.GetByEmailAsync(userRegisterDto.Email ?? string.Empty);
        if (existingUser != null)
        {
            this.logger.LogWarning("Registration failed - user already exists: {Email}", userRegisterDto.Email?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            throw new InvalidOperationException($"User with email {userRegisterDto.Email} already exists.");
        }

        var hashedPassword = this.passwordHasher.HashPassword(userRegisterDto.Password);

        var user = new User()
        {
            Email = userRegisterDto.Email ?? string.Empty,
            UserName = userRegisterDto.UserName ?? string.Empty,
            Password = hashedPassword,
        };

        await this.userRepository.AddAsync(user);

        this.logger.LogInformation("User created successfully: {UserId}, Email: {Email}", user.Id, user.Email?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

        // Generate tokens
        var accessToken = this.jwtService.GenerateAccessToken(user);
        var refreshToken = this.jwtService.GenerateRefreshToken(user);

        this.logger.LogDebug("Generated tokens for user: {UserId}", user.Id);

        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await this.refreshTokenRepository.AddAsync(refreshTokenEntity);

        try
        {
            await this.authService.SendVerificationCodeAsync(user.Id);
            this.logger.LogInformation("Verification code sent to user: {Email}", user.Email?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to send verification code to user {Email}", user.Email?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
        }

        return new AuthResponseDto
        {
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                ServerId = user.ServerId,
            },
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 1800 // 30 minutes
        };
    }

    public async Task<UserData> UpdateUserAsync(Guid id, UserUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        this.logger.LogInformation("Updating user: {UserId}", id);

        var existingUser = await this.userRepository
                               .GetByIdAsync(id)
                           ?? throw new KeyNotFoundException($"User with ID {id} not found");

        existingUser.UserName = updateDto.UserName;
        existingUser.Email = updateDto.Email;

        // Only update password if provided
        if (!string.IsNullOrEmpty(updateDto.Password))
        {
            this.logger.LogInformation("Password changed for user: {UserId}", id);
            existingUser.Password = this.passwordHasher.HashPassword(updateDto.Password);
        }

        existingUser.ServerId = updateDto.ServerId;

        await this.userRepository.UpdateUserAsync(existingUser);

        this.logger.LogInformation("User updated successfully: {UserId}", id);

        return new UserData
        {
            Id = existingUser.Id,
            UserName = existingUser.UserName,
            Email = existingUser.Email ?? string.Empty,
            ServerId = existingUser.ServerId,
            IsEmailVerified = existingUser.IsEmailVerified,
            EmailVerifiedAt = existingUser.EmailVerifiedAt,
        };
    }

    public async Task DeleteUserAsync(Guid id)
    {
        this.logger.LogWarning("Deleting user: {UserId}", id);
        await this.userRepository.DeleteAsync(id);
        this.logger.LogInformation("User deleted successfully: {UserId}", id);
    }

    public async Task<bool> CanUserAccessProtectedResourcesAsync(Guid userId)
    {
        var user = await this.userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        // Check if email verification is disabled (for testing)
        var requireEmailVerification = this.configuration.GetValue("Authentication:RequireEmailVerification", true);
        if (!requireEmailVerification)
        {
            return true;
        }

        // OAuth users without email (Telegram) - access allowed
        if (!user.HasPassword && string.IsNullOrEmpty(user.Email))
        {
            return true;
        }

        // Users with email - require verification
        if (!string.IsNullOrEmpty(user.Email))
        {
            return user.IsEmailVerified;
        }

        return true;
    }

    public async Task<AuthResponseDto> LoginUserAsync(UserLoginDto userLoginDto)
    {
        ArgumentNullException.ThrowIfNull(userLoginDto);

        this.logger.LogInformation("Login attempt for email: {Email}", userLoginDto.Email?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

        var user = await this.userRepository.GetByEmailAsync(userLoginDto.Email ?? string.Empty);
        if (user == null || !this.passwordHasher.VerifyPassword(userLoginDto.Password, user.Password))
        {
            this.logger.LogWarning("Failed login attempt for email: {Email} - invalid credentials", userLoginDto.Email?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            throw new KeyNotFoundException($"Invalid email or password");
        }

        // Check if user can access protected resources
        if (!await this.CanUserAccessProtectedResourcesAsync(user.Id))
        {
            this.logger.LogWarning("Login blocked for unverified email: {Email}, UserId: {UserId}", user.Email?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", user.Id);
            throw new UnauthorizedAccessException("Email verification required. Please check your email for verification code.");
        }

        // Generate tokens
        var accessToken = this.jwtService.GenerateAccessToken(user);
        var refreshToken = this.jwtService.GenerateRefreshToken(user);

        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await this.refreshTokenRepository.AddAsync(refreshTokenEntity);

        this.logger.LogInformation("Login successful for user: {Email}, UserId: {UserId}", user.Email?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", user.Id);

        return new AuthResponseDto
        {
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName,
                ServerId = user.ServerId,
            },
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 1800 // 30 minutes
        };
    }
}
