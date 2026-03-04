namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService jwtService;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly IAuthService authService;
    private readonly IOAuthService oauthService;
    private readonly ILogger<AuthController> logger;

    public AuthController(IJwtService jwtService, IRefreshTokenRepository refreshTokenRepository, IAuthService authService, IOAuthService oauthService, ILogger<AuthController> logger)
    {
        this.jwtService = jwtService;
        this.refreshTokenRepository = refreshTokenRepository;
        this.authService = authService;
        this.oauthService = oauthService;
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

    [HttpPost("request-account-deletion")]
    [Authorize]
    public async Task<ActionResult> RequestAccountDeletion([FromBody] RequestAccountDeletionDto request)
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            await this.authService.RequestAccountDeletionAsync(userId, request.Password);
            return this.Ok(new { message = "Account deletion confirmation code sent to your email" });
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error requesting account deletion");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("confirm-account-deletion")]
    [Authorize]
    public async Task<ActionResult> ConfirmAccountDeletion([FromBody] ConfirmAccountDeletionDto request)
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            var result = await this.authService.ConfirmAccountDeletionAsync(userId, request.ConfirmationCode);
            if (!result)
            {
                return this.BadRequest(new { message = "Invalid or expired confirmation code" });
            }

            return this.Ok(new { message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error confirming account deletion");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    #region OAuth2 Endpoints

    [HttpGet("oauth/{provider}/challenge")]
    public async Task<ActionResult<OAuthChallengeResponseDto>> CreateOAuthChallenge(string provider, [FromQuery] Uri? returnUrl = null)
    {
        try
        {
            this.logger.LogInformation("OAuth challenge request received - Provider: {Provider}, ReturnUrl: {ReturnUrl}", provider, returnUrl?.ToString() ?? "null");

            var oauthProvider = this.oauthService.ParseProvider(provider);
            this.logger.LogInformation("Parsed OAuth provider: {OAuthProvider}", oauthProvider);
            
            if (!this.oauthService.IsProviderEnabled(oauthProvider))
            {
                this.logger.LogWarning("OAuth provider {Provider} is not enabled", provider);
                return this.BadRequest(new { message = $"OAuth provider {provider} is not enabled" });
            }

            this.logger.LogInformation("Creating OAuth challenge for provider: {OAuthProvider}", oauthProvider);
            var challenge = await this.oauthService.CreateChallengeAsync(oauthProvider, returnUrl);
            
            this.logger.LogInformation(
                "OAuth challenge created successfully - Provider: {OAuthProvider}, ChallengeUrl: {ChallengeUrl}",
                oauthProvider,
                challenge.ChallengeUrl.ToString()[..Math.Min(challenge.ChallengeUrl.ToString().Length, 100)] + "...");
            
            return this.Ok(challenge);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error creating OAuth challenge for provider: {Provider}", provider);
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("oauth/callback")]
    public async Task<ActionResult<AuthResponseDto>> OAuthCallback([FromBody] OAuthCallbackRequestDto request)
    {
        try
        {
            var ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = this.HttpContext.Request.Headers.UserAgent.ToString();

            var response = await this.oauthService.ProcessCallbackAsync(request, ipAddress, userAgent);
            return this.Ok(response);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing OAuth callback for provider: {Provider}", request.Provider);
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("oauth/callback")]
    public async Task<IActionResult> OAuthCallbackGet([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? hash, [FromQuery] string? provider, [FromQuery] bool linkingAction = false, [FromQuery] string? userId = null)
    {
        try
        {
            var ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = this.HttpContext.Request.Headers.UserAgent.ToString();

            // Handle Telegram OAuth special case - Telegram uses 'hash' instead of 'code'
            if (!string.IsNullOrEmpty(hash) && string.IsNullOrEmpty(code))
            {
                code = hash; // Use hash as code for Telegram
            }

            // If provider is not specified, try to determine from state
            if (string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(state))
            {
                provider = DetermineProviderFromState(state);
                this.logger.LogInformation("Provider determined from state: {Provider}", provider);
            }

            this.logger.LogInformation(
            "OAuth callback received - Provider: {Provider}, Code: {Code}, Hash: {Hash}, State: {State}, LinkingAction: {LinkingAction}",
            provider,
            code?.Length > 10 ? $"{code[..10]}..." : code ?? "null",
            hash?.Length > 10 ? $"{hash[..10]}..." : hash ?? "null",
            state,
            linkingAction);

            // Validate required parameters
            if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(hash))
            {
                this.logger.LogWarning("OAuth callback missing required parameters - Code: {Code}, Hash: {Hash}, State: {State}", code, hash, state);
                return this.BadRequest(new { message = "Missing authentication parameters" });
            }

            if (string.IsNullOrEmpty(state))
            {
                this.logger.LogWarning("OAuth callback missing state parameter");
                return this.BadRequest(new { message = "Missing state parameter" });
            }

            var request = new OAuthCallbackRequestDto
            {
                Provider = provider ?? string.Empty,
                Code = code ?? hash ?? string.Empty, // Use code or hash for Telegram
                State = ExtractStateFromState(state), // Remove provider prefix if present
                LinkingAction = linkingAction,
                UserId = userId
            };

            // If this is a linking action, use LinkExternalLoginAsync instead
            if (linkingAction && !string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
            {
                this.logger.LogInformation("Processing OAuth linking for user: {UserId}", userGuid);
                
                var linkRequest = new OAuthLinkRequestDto
                {
                    Provider = request.Provider,
                    Code = request.Code,
                    State = request.State
                };
                
                var linkResponse = await this.oauthService.LinkExternalLoginAsync(userGuid, linkRequest, ipAddress, userAgent);
                
                this.logger.LogInformation("OAuth linking successful for user: {UserId}", userGuid);
                
                // Redirect to connected accounts page with success
                return this.Redirect("http://localhost:3001/settings/connected-accounts?linking=success");
            }

            var response = await this.oauthService.ProcessCallbackAsync(request, ipAddress, userAgent);

            this.logger.LogInformation(
            "OAuth callback processed successfully - User: {UserId}, Token: {Token}",
            response.User?.Id,
            response.Token.Length > 20 ? $"{response.Token[..20]}..." : response.Token);

            // Set JWT tokens in cookies with correct domain for frontend
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(1),

                // Domain removed - browser will set it automatically for the current host
                Path = "/"
            };

            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7),

                // Domain removed - browser will set it automatically for the current host
                Path = "/"
            };

            this.Response.Cookies.Append("access_token", response.Token, cookieOptions);
            this.Response.Cookies.Append("refresh_token", response.RefreshToken ?? string.Empty, refreshTokenOptions);

            this.logger.LogInformation(
            "JWT cookies set for OAuth callback - Access token expires: {Expires}, Refresh token expires: {RefreshExpires}",
            cookieOptions.Expires,
            refreshTokenOptions.Expires);

            // Redirect to frontend
            return this.Redirect("http://localhost:3001/dashboard?auth=success");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing OAuth callback for provider: {Provider}", provider);
            return this.Redirect("http://localhost:3001/auth?error=oauth_failed");
        }
    }

    [HttpGet("oauth/providers")]
    [Authorize]
    public async Task<ActionResult<List<OAuthProviderInfoDto>>> GetUserExternalLogins()
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            var providers = await this.oauthService.GetUserExternalLoginsAsync(userId);
            return this.Ok(providers);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting user external logins");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("oauth/link")]
    [Authorize]
    public async Task<ActionResult<AuthResponseDto>> LinkExternalLogin([FromBody] OAuthLinkRequestDto request)
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            var ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = this.HttpContext.Request.Headers.UserAgent.ToString();

            var response = await this.oauthService.LinkExternalLoginAsync(userId, request, ipAddress, userAgent);
            return this.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning(ex, "Error linking external login: {Message}", ex.Message);
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error linking external login");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("oauth/{provider}")]
    [Authorize]
    public async Task<ActionResult> UnlinkExternalLogin(string provider)
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.BadRequest(new { message = "Invalid user identifier" });
            }

            var oauthProvider = this.oauthService.ParseProvider(provider);
            await this.oauthService.UnlinkExternalLoginAsync(userId, oauthProvider);

            return this.Ok(new { message = "External login unlinked successfully" });
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning(ex, "Error unlinking external login: {Message}", ex.Message);
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error unlinking external login");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("oauth/telegram/callback")]
    public async Task<IActionResult> TelegramCallbackPost([FromBody] TelegramCallbackRequestDto request)
    {
        try
        {
            var ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = this.HttpContext.Request.Headers.UserAgent.ToString();

            this.logger.LogInformation(
                "Telegram OAuth callback received - User ID: {UserId}, State: {State}",
                request.UserData?.Id,
                request.State);

            // Convert Telegram user data to OAuth format
            var telegramCode = JsonSerializer.Serialize(request.UserData);
            
            // For Telegram, always generate a temporary state
            // Telegram doesn't return state in callback, so we need to handle this
            var state = $"telegram_temp_{Guid.NewGuid():N}";
            
            this.logger.LogInformation("Generated temporary state for Telegram OAuth - State: {State}", state);
            
            var oauthRequest = new OAuthCallbackRequestDto
            {
                Provider = "telegram",
                Code = telegramCode,
                State = state
            };

            var response = await this.oauthService.ProcessCallbackAsync(oauthRequest, ipAddress, userAgent);

            this.logger.LogInformation(
                "Telegram OAuth callback processed successfully - User: {UserId}, Token: {Token}",
                response.User?.Id,
                response.Token.Length > 20 ? $"{response.Token[..20]}..." : response.Token);

            // Return tokens in response body for frontend to handle
            // Frontend will redirect to localhost:3001 with tokens
            return this.Ok(new
            {
                success = true,
                message = "Authentication successful",
                token = response.Token,
                refreshToken = response.RefreshToken,
                user = new
                {
                    id = response.User?.Id,
                    email = response.User?.Email,
                    username = response.User?.UserName
                }
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing Telegram OAuth callback");
            return this.StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private static string DetermineProviderFromState(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            return "google"; // Default fallback
        }

        if (state.StartsWith("github_", StringComparison.OrdinalIgnoreCase))
        {
            return "github";
        }

        if (state.StartsWith("google_", StringComparison.OrdinalIgnoreCase))
        {
            return "google";
        }

        if (state.StartsWith("telegram_", StringComparison.OrdinalIgnoreCase))
        {
            return "telegram";
        }

        return "google"; // Default fallback
    }

    private static string ExtractStateFromState(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            return state;
        }

        var underscoreIndex = state.IndexOf('_', StringComparison.Ordinal);
        if (underscoreIndex > 0 && underscoreIndex < state.Length - 1)
        {
            return state[(underscoreIndex + 1)..];
        }

        return state;
    }

    #endregion
}
