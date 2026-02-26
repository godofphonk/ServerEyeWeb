namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService jwtService;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly IAuthService authService;
    private readonly ILogger<AuthController> logger;

    public AuthController(IJwtService jwtService, IRefreshTokenRepository refreshTokenRepository, IAuthService authService, ILogger<AuthController> logger)
    {
        this.jwtService = jwtService;
        this.refreshTokenRepository = refreshTokenRepository;
        this.authService = authService;
        this.logger = logger;
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrEmpty(request.Token))
            {
                return this.BadRequest(new { message = "Token is required" });
            }

            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return this.BadRequest(new { message = "Refresh token is required" });
            }

            this.logger.LogInformation(
                "Refresh token request received. Token length: {TokenLength}, RefreshToken length: {RefreshTokenLength}",
                request.Token.Length,
                request.RefreshToken.Length);

            // Validate the access token (even if expired, we can extract user info)
            var principal = this.jwtService.ValidateToken(request.Token);
            if (principal == null)
            {
                this.logger.LogWarning("Token validation failed for refresh request");
                return this.BadRequest(new { message = "Invalid token" });
            }

            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier in token" });
            }

            // Get refresh token from database
            var refreshTokenEntity = await this.refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
            if (refreshTokenEntity == null)
            {
                this.logger.LogWarning("Refresh token not found in database for user {UserId}", userId);
                return this.BadRequest(new { message = "Invalid or expired refresh token" });
            }

            if (refreshTokenEntity.UserId != userId)
            {
                this.logger.LogWarning("Refresh token user mismatch. Expected: {ExpectedUserId}, Actual: {ActualUserId}", userId, refreshTokenEntity.UserId);
                return this.BadRequest(new { message = "Invalid or expired refresh token" });
            }

            if (refreshTokenEntity.IsRevoked)
            {
                this.logger.LogWarning("Refresh token is revoked for user {UserId}", userId);
                return this.BadRequest(new { message = "Invalid or expired refresh token" });
            }

            // Get user (we need user entity to generate new tokens)
            // This is a simplified approach - in production, you might want to cache user data
            var user = new User
            {
                Id = userId,
                Email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
                UserName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty
            };

            // Revoke old refresh token
            await this.refreshTokenRepository.RevokeTokenAsync(refreshTokenEntity.Id);

            // Generate new tokens
            var newAccessToken = this.jwtService.GenerateAccessToken(user);
            var newRefreshToken = this.jwtService.GenerateRefreshToken(user);

            this.logger.LogInformation("Generated new access token for user {UserId}. Token length: {TokenLength}", userId, newAccessToken.Length);

            // Save new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await this.refreshTokenRepository.AddAsync(newRefreshTokenEntity);

            this.logger.LogInformation("Token refreshed for user {UserId}", userId);

            return this.Ok(new AuthResponseDto
            {
                User = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    ServerId = Guid.Empty // Default value for now
                },
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 1800 // 30 minutes
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error refreshing token");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            // Revoke all refresh tokens for this user
            await this.refreshTokenRepository.RevokeAllUserTokensAsync(userId);

            this.logger.LogInformation("User {UserId} logged out", userId);

            return this.Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during logout");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<ActionResult> RevokeToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return this.BadRequest(new { message = "Refresh token is required" });
            }

            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            // Find the refresh token
            var refreshTokenEntity = await this.refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
            if (refreshTokenEntity == null || refreshTokenEntity.UserId != userId)
            {
                return this.BadRequest(new { message = "Invalid refresh token" });
            }

            // Revoke the specific refresh token
            await this.refreshTokenRepository.RevokeTokenAsync(refreshTokenEntity.Id);

            this.logger.LogInformation("Token revoked for user {UserId}", userId);

            return this.Ok(new { message = "Token revoked successfully" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error revoking token");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("verify-email")]
    [Authorize]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            var result = await this.authService.VerifyEmailAsync(userId, request.Code);
            if (!result)
            {
                return this.BadRequest(new { message = "Invalid or expired verification code" });
            }

            this.logger.LogInformation("Email verified for user {UserId}", userId);
            return this.Ok(new { message = "Email verified successfully" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error verifying email");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<ActionResult> ResendVerification()
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            await this.authService.SendVerificationCodeAsync(userId);
            this.logger.LogInformation("Verification code resent to user {UserId}", userId);
            return this.Ok(new { message = "Verification code sent" });
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error resending verification code");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
    {
        try
        {
            await this.authService.RequestPasswordResetAsync(request.Email);
            return this.Ok(new { message = "If the email exists, a password reset link has been sent" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing forgot password request");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        try
        {
            var result = await this.authService.ResetPasswordAsync(request.Token, request.NewPassword);
            if (!result)
            {
                return this.BadRequest(new { message = "Invalid or expired reset token" });
            }

            this.logger.LogInformation("Password reset successfully");
            return this.Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error resetting password");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("change-email")]
    [Authorize]
    public async Task<ActionResult> ChangeEmail([FromBody] ChangeEmailDto request)
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            await this.authService.RequestEmailChangeAsync(userId, request.NewEmail);
            this.logger.LogInformation("Email change requested for user {UserId}", userId);
            return this.Ok(new { message = "Verification code sent to new email" });
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error requesting email change");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("confirm-email-change")]
    [Authorize]
    public async Task<ActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeDto request)
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            var result = await this.authService.ConfirmEmailChangeAsync(userId, request.Code);
            if (!result)
            {
                return this.BadRequest(new { message = "Invalid or expired verification code" });
            }

            this.logger.LogInformation("Email changed successfully for user {UserId}", userId);
            return this.Ok(new { message = "Email changed successfully" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error confirming email change");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }
}
