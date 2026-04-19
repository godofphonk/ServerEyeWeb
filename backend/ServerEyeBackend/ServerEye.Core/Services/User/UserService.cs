namespace ServerEye.Core.Services;

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.DTOs.UserDto;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Helpers;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Interfaces.Services.Billing;

public sealed class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtService jwtService, IRefreshTokenRepository refreshTokenRepository, IAuthService authService, IConfiguration configuration, ILogger<UserService> logger, IPlanLimitsService planLimitsService, IMetricsCacheService cacheService, CacheSettings cacheSettings) : IUserService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly IJwtService jwtService = jwtService;
    private readonly IRefreshTokenRepository refreshTokenRepository = refreshTokenRepository;
    private readonly IPlanLimitsService planLimitsService = planLimitsService;
    private readonly IAuthService authService = authService;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger<UserService> logger = logger;
    private readonly IMetricsCacheService cacheService = cacheService;
    private readonly CacheSettings cacheSettings = cacheSettings;

    public async Task<UserData?> GetUserByIdAsync(Guid id)
    {
        var cacheKey = $"user:{id}";
        var cachedResult = await this.cacheService.GetAsync<UserData>(cacheKey);

        if (cachedResult != null)
        {
            this.logger.LogDebug("Cache hit for user profile: {UserId}", id);
            return cachedResult;
        }

        var user = await this.userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        var limits = await this.planLimitsService.GetUserLimitsAsync(id);

        var result = new UserData()
        {
            Email = user.Email ?? string.Empty,
            Id = user.Id,
            UserName = user.UserName,
            Role = user.Role.ToString().ToUpperInvariant(),
            ServerId = user.ServerId,
            IsEmailVerified = user.IsEmailVerified,
            EmailVerifiedAt = user.EmailVerifiedAt,
            CreatedAt = user.CreatedAt,
            MaxServers = limits.MaxServers,
            CurrentServers = limits.CurrentServers,
            MetricsRetentionDays = limits.MetricsRetentionDays,
            PlanName = limits.PlanName,
            PlanType = limits.PlanType.ToString(),
            HasActiveSubscription = limits.HasActiveSubscription,
        };

        await this.cacheService.SetAsync(cacheKey, result, this.cacheSettings.UserProfile);

        return result;
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
            CreatedAt = user.CreatedAt,
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
            CreatedAt = user.CreatedAt,
        }).ToList();
    }

    public async Task<AuthResponseDto> CreateUserAsync(UserRegisterDto userRegisterDto)
    {
        ArgumentNullException.ThrowIfNull(userRegisterDto);

        this.logger.LogInformation("Starting user registration for email: {Email}, username: {UserName}", LogSanitizer.MaskEmail(userRegisterDto.Email), LogSanitizer.Sanitize(userRegisterDto.UserName));

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
            CreatedAt = DateTime.UtcNow,
        };

        await this.userRepository.AddAsync(user);

        this.logger.LogInformation("User created successfully: {UserId}, Email: {Email}", user.Id, LogSanitizer.MaskEmail(user.Email));

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
            this.logger.LogInformation("Verification code sent to user: {Email}", LogSanitizer.MaskEmail(user.Email));
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
                IsEmailVerified = user.IsEmailVerified,
                RequiresEmailVerification = user.HasPassword && !user.IsEmailVerified && !string.IsNullOrEmpty(user.Email),
                CreatedAt = user.CreatedAt
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

        // Invalidate cache for user profile
        await this.cacheService.RemoveAsync($"user:{id}");

        this.logger.LogInformation("User updated successfully: {UserId}", id);

        return new UserData
        {
            Id = existingUser.Id,
            UserName = existingUser.UserName,
            Email = existingUser.Email ?? string.Empty,
            ServerId = existingUser.ServerId,
            IsEmailVerified = existingUser.IsEmailVerified,
            EmailVerifiedAt = existingUser.EmailVerifiedAt,
            CreatedAt = existingUser.CreatedAt,
        };
    }

    public async Task DeleteUserAsync(Guid id)
    {
        this.logger.LogWarning("Deleting user: {UserId}", id);

        // Invalidate cache for user profile
        await this.cacheService.RemoveAsync($"user:{id}");

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

        // OAuth users - access allowed without email verification
        if (!user.HasPassword)
        {
            // OAuth users (Google, GitHub, Telegram) don't need email verification
            this.logger.LogDebug("OAuth user {UserId} accessing protected resources - email verification skipped", userId);
            return true;
        }

        // Regular users with password - require email verification
        if (!string.IsNullOrEmpty(user.Email))
        {
            return user.IsEmailVerified;
        }

        return true;
    }

    public async Task<AuthResponseDto> LoginUserAsync(UserLoginDto userLoginDto)
    {
        ArgumentNullException.ThrowIfNull(userLoginDto);

        this.logger.LogInformation("Login attempt for email: {Email}", LogSanitizer.MaskEmail(userLoginDto.Email));

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

        // Save refresh token - 30 days if rememberMe, else 7 days
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = userLoginDto.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await this.refreshTokenRepository.AddAsync(refreshTokenEntity);

        // CodeQL[cs/log-injection] suppress: "log entries created from user input" reason: email is masked via LogSanitizer.MaskEmail
        this.logger.LogInformation("Login successful for user: {Email}, UserId: {UserId}, RememberMe: {RememberMe}", LogSanitizer.MaskEmail(user.Email), user.Id, userLoginDto.RememberMe);

        return new AuthResponseDto
        {
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName,
                ServerId = user.ServerId,
                IsEmailVerified = user.IsEmailVerified,
                RequiresEmailVerification = user.HasPassword && !user.IsEmailVerified && !string.IsNullOrEmpty(user.Email),
                CreatedAt = user.CreatedAt
            },
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 1800, // 30 minutes
            RememberMe = userLoginDto.RememberMe
        };
    }

    public async Task<AuthResponseDto> LoginWithTwoFactorAsync(string email, string code, bool rememberMe = false)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(code);

        // CodeQL[cs/log-injection] suppress: "log entries created from user input" reason: email is masked via LogSanitizer.MaskEmail
        this.logger.LogInformation("2FA attempt for email: {Email}", LogSanitizer.MaskEmail(email));

        var user = await this.userRepository.GetByEmailAsync(email);
        ArgumentNullException.ThrowIfNull(user, "User not found");

        // Проверяем код
        var result = await this.authService.VerifyEmailAsync(user.Id, code);
        if (!result)
        {
            // CodeQL[cs/log-injection] suppress: "log entries created from user input" reason: email is masked via LogSanitizer.MaskEmail
            this.logger.LogWarning("Failed 2FA attempt for email: {Email} - invalid code", LogSanitizer.MaskEmail(email));
            throw new UnauthorizedAccessException("Invalid or expired verification code");
        }

        // Generate tokens
        var accessToken = this.jwtService.GenerateAccessToken(user);
        var refreshToken = this.jwtService.GenerateRefreshToken(user);

        // Save refresh token - 30 days if rememberMe, else 7 days
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await this.refreshTokenRepository.AddAsync(refreshTokenEntity);

        // CodeQL[cs/log-injection] suppress: "log entries created from user input" reason: email is masked via LogSanitizer.MaskEmail
        this.logger.LogInformation("Login successful for user: {Email}, UserId: {UserId}, RememberMe: {RememberMe}", LogSanitizer.MaskEmail(user.Email), user.Id, rememberMe);

        return new AuthResponseDto
        {
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName,
                ServerId = user.ServerId,
                IsEmailVerified = user.IsEmailVerified,
                RequiresEmailVerification = user.HasPassword && !user.IsEmailVerified && !string.IsNullOrEmpty(user.Email),
                CreatedAt = user.CreatedAt
            },
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 1800, // 30 minutes
            RememberMe = rememberMe
        };
    }
}
