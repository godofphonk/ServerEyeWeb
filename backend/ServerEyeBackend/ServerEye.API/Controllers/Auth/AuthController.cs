namespace ServerEye.API.Controllers;

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.API.Helpers;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Entities;
using ServerEye.Core.Helpers;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Services.OAuth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IJwtService jwtService;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly IAuthService authService;
    private readonly IOAuthService oauthService;
    private readonly IUserRepository userRepository;
    private readonly ILogger<AuthController> logger;
    private readonly FrontendSettings frontendSettings;
    private readonly OAuthMetrics metrics;

    public AuthController(IJwtService jwtService, IRefreshTokenRepository refreshTokenRepository, IAuthService authService, IOAuthService oauthService, IUserRepository userRepository, ILogger<AuthController> logger, FrontendSettings frontendSettings, OAuthMetrics metrics)
    {
        this.jwtService = jwtService;
        this.refreshTokenRepository = refreshTokenRepository;
        this.authService = authService;
        this.oauthService = oauthService;
        this.userRepository = userRepository;
        this.logger = logger;
        this.frontendSettings = frontendSettings;
        this.metrics = metrics;
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
    public async Task<ActionResult<OAuthChallengeResponseDto>> CreateOAuthChallenge(string provider, [FromQuery] Uri? returnUrl = null, [FromQuery] string? action = null)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            this.logger.LogInformation("OAuth challenge request received - Provider: {Provider}, ReturnUrl: {ReturnUrl}, Action: {Action}", LogSanitizer.Sanitize(provider) ?? "null", LogSanitizer.Sanitize(returnUrl?.ToString()) ?? "null", LogSanitizer.Sanitize(action) ?? "null");

            var oauthProvider = this.oauthService.ParseProvider(provider ?? string.Empty);
            this.logger.LogInformation("Parsed OAuth provider: {OAuthProvider}", oauthProvider.ToString().Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal));

            if (!this.oauthService.IsProviderEnabled(oauthProvider))
            {
                this.logger.LogWarning("OAuth provider {Provider} is not enabled", (provider ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
                this.metrics.RecordError(provider ?? string.Empty, "controller_challenge", "provider_disabled");
                return this.BadRequest(new { message = $"OAuth provider {(provider ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null"} is not enabled" });
            }

            this.logger.LogInformation("Creating OAuth challenge for provider: {OAuthProvider} with action: {Action}", oauthProvider.ToString().Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal), LogSanitizer.Sanitize(action) ?? "auto");
            var challenge = await this.oauthService.CreateChallengeAsync(oauthProvider, returnUrl, action);

            this.logger.LogInformation(
                "OAuth challenge created successfully - Provider: {OAuthProvider}, Action: {Action}, ChallengeUrl: {ChallengeUrl}",
                oauthProvider,
                LogSanitizer.Sanitize(action) ?? "auto",
                challenge.ChallengeUrl.ToString()[..Math.Min(challenge.ChallengeUrl.ToString().Length, 100)] + "...");

            // Record controller-level metrics
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            this.metrics.RecordChallengeCreated(provider ?? string.Empty, action);

            return this.Ok(challenge);
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            this.metrics.RecordError(provider ?? string.Empty, "controller_challenge", ex.GetType().Name, ex.Message);
            this.logger.LogError(ex, "Error creating OAuth challenge for provider: {Provider}", (provider ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("session")]
    public async Task<ActionResult<object>> GetSession()
    {
        try
        {
            var userId = this.GetUserId();
            var user = await this.userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                return Ok(new { user = (object?)null });
            }

            return Ok(new { user });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting session");
            return Ok(new { user = (object?)null });
        }
    }

    [HttpPost("session")]
    public async Task<ActionResult<object>> SetSession()
    {
        try
        {
            var userId = this.GetUserId();
            var user = await this.userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                return Ok(new { user = (object?)null });
            }

            return Ok(new { user });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error setting session");
            return Ok(new { user = (object?)null });
        }
    }

    [HttpPost("oauth/callback")]
    public async Task<ActionResult<AuthResponseDto>> OAuthCallback([FromBody] OAuthCallbackRequestDto request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = this.HttpContext.Request.Headers.UserAgent.ToString();

            // Code verifier is now stored in Redis by OAuthService, no need for manual storage
            var response = await this.oauthService.ProcessCallbackAsync(request, ipAddress, userAgent);

            // Record success metrics
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            this.metrics.RecordTokenExchange(request.Provider, true);
            this.metrics.RecordTokenExchangeDuration(request.Provider, duration, true);

            return this.Ok(response);
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            this.metrics.RecordError(request.Provider, "controller_callback", ex.GetType().Name, ex.Message);
            this.metrics.RecordTokenExchange(request.Provider, false);
            this.metrics.RecordTokenExchangeDuration(request.Provider, duration, false);
            this.logger.LogError(ex, "Error processing OAuth callback for provider: {Provider}", request.Provider);
            return this.StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("oauth/callback")]
    public async Task<IActionResult> OAuthCallbackGet([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? hash, [FromQuery] string? provider, [FromQuery] bool linkingAction = false, [FromQuery] string? userId = null, [FromQuery] string? action = null)
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

            // Parse linking state first
            var (isLinking, linkingProvider, linkingUserId, actualState) = ParseLinkingState(state ?? string.Empty);

            this.logger.LogInformation(
                "OAuth callback received - Code: {Code}, Hash: {Hash}, State: {State}, Action: {Action}, IsLinking: {IsLinking}, LinkingProvider: {LinkingProvider}, LinkingUserId: {LinkingUserId}",
                LogSanitizer.MaskToken(code, 10),
                LogSanitizer.MaskToken(hash, 10),
                LogSanitizer.Sanitize(state) ?? "null",
                LogSanitizer.Sanitize(action) ?? "auto",
                isLinking,
                LogSanitizer.Sanitize(linkingProvider) ?? "null",
                LogSanitizer.MaskToken(linkingUserId, 8));

            // Validate required parameters
            if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(hash))
            {
                this.logger.LogWarning("OAuth callback missing required parameters - Code: {Code}, Hash: {Hash}, State: {State}", LogSanitizer.MaskToken(code, 10), LogSanitizer.MaskToken(hash, 10), LogSanitizer.Sanitize(state) ?? "null");
                return this.BadRequest(new { message = "Missing authentication parameters" });
            }

            if (string.IsNullOrEmpty(state))
            {
                this.logger.LogWarning("OAuth callback missing state parameter");
                return this.BadRequest(new { message = "Missing state parameter" });
            }

            // If this is a linking request from state, use LinkExternalLoginAsync
            if (isLinking && !string.IsNullOrEmpty(linkingProvider) && !string.IsNullOrEmpty(linkingUserId))
            {
                this.logger.LogInformation("Processing OAuth linking from state - Provider: {Provider}, UserId: {UserId}", linkingProvider, linkingUserId);

                if (Guid.TryParse(linkingUserId, out var userGuid))
                {
                    try
                    {
                        var linkRequest = new OAuthLinkRequestDto
                        {
                            Provider = linkingProvider,
                            Code = code ?? hash ?? string.Empty,
                            State = actualState
                        };

                        var linkResponse = await this.oauthService.LinkExternalLoginAsync(userGuid, linkRequest, ipAddress, userAgent);

                        this.logger.LogInformation("OAuth linking successful for user: {UserId}", userGuid);

                        // Redirect to profile page with success
                        return this.Redirect($"{this.frontendSettings.BaseUrl}profile?linking=success");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("already linked to another user", StringComparison.OrdinalIgnoreCase))
                    {
                        this.logger.LogWarning("OAuth linking failed - external account already linked to another user");
                        return this.Redirect($"{this.frontendSettings.BaseUrl}profile?error=already_linked");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("already linked to", StringComparison.OrdinalIgnoreCase))
                    {
                        this.logger.LogWarning("OAuth linking failed - account already linked: {Message}", ex.Message);
                        return this.Redirect($"{this.frontendSettings.BaseUrl}profile?error=already_linked");
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "OAuth linking failed with unexpected error");
                        return this.Redirect($"{this.frontendSettings.BaseUrl}profile?error=linking_failed");
                    }
                }
                else
                {
                    this.logger.LogWarning("Invalid user ID in linking state: {UserId}", linkingUserId);
                    return this.BadRequest(new { message = "Invalid user ID in linking state" });
                }
            }

            var request = new OAuthCallbackRequestDto
            {
                Provider = provider ?? string.Empty,
                Code = code ?? hash ?? string.Empty, // Use code or hash for Telegram
                State = isLinking && !string.IsNullOrEmpty(actualState) ? actualState : ExtractStateFromState(state), // Use actualState from linking or extract from regular state
                LinkingAction = linkingAction || isLinking, // Set to true if linking detected from state OR query parameter
                UserId = userId ?? linkingUserId, // Use userId from query parameter OR from linking state
                Action = action // Pass action parameter from query
            };

            // Fallback to parameter-based linking (if state doesn't contain linking info)
            // SECURITY: First validate authentication before processing any user-provided GUID
            if (linkingAction && !string.IsNullOrEmpty(userId))
            {
                var authenticatedUserId = this.GetUserId();
                if (authenticatedUserId == Guid.Empty)
                {
                    this.logger.LogWarning("Security: OAuth linking attempted without authentication");
                    return this.Unauthorized(new { message = "Authentication required for OAuth linking" });
                }

                // Only parse user-provided GUID after authentication is confirmed
                if (!Guid.TryParse(userId, out var parameterUserGuid))
                {
                    this.logger.LogWarning("Security: Invalid GUID format provided for OAuth linking");
                    return this.BadRequest(new { message = "Invalid user ID format" });
                }

                // SECURITY: Validate that the userId parameter matches the authenticated user
                // This prevents user-controlled bypass where an attacker could link OAuth to another user's account
                if (authenticatedUserId != parameterUserGuid)
                {
                    this.logger.LogWarning("Security: Attempted OAuth linking with mismatched user ID - Authenticated: {AuthUserId}, Parameter: {ParamUserId}", authenticatedUserId, parameterUserGuid);
                    return this.Unauthorized(new { message = "User ID mismatch - authentication required" });
                }

                this.logger.LogInformation("Processing OAuth linking from parameters - User: {UserId}", parameterUserGuid);

                try
                {
                    var linkRequest = new OAuthLinkRequestDto
                    {
                        Provider = request.Provider,
                        Code = request.Code,
                        State = request.State
                    };

                    var linkResponse = await this.oauthService.LinkExternalLoginAsync(parameterUserGuid, linkRequest, ipAddress, userAgent);

                    this.logger.LogInformation("OAuth linking successful for user: {UserId}", parameterUserGuid);

                    // Redirect to profile page with success
                    return this.Redirect($"{this.frontendSettings.BaseUrl}profile?linking=success");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("already linked to another user", StringComparison.OrdinalIgnoreCase))
                {
                    this.logger.LogWarning("OAuth linking failed - external account already linked to another user");
                    return this.Redirect($"{this.frontendSettings.BaseUrl}profile?error=already_linked");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("already linked to", StringComparison.OrdinalIgnoreCase))
                {
                    this.logger.LogWarning("OAuth linking failed - account already linked: {Message}", ex.Message);
                    return this.Redirect($"{this.frontendSettings.BaseUrl}profile?error=already_linked");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "OAuth linking failed with unexpected error");
                    return this.Redirect($"{this.frontendSettings.BaseUrl}profile?error=linking_failed");
                }
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
                Secure = true, // Always use secure cookies for JWT tokens
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(1),

                // Domain removed - browser will set it for current host
                Path = "/"
            };

            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Always use secure cookies for JWT tokens
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7),

                // Domain removed - browser will set it for current host
                Path = "/"
            };

            this.Response.Cookies.Append("access_token", response.Token, cookieOptions);
            this.Response.Cookies.Append("refresh_token", response.RefreshToken ?? string.Empty, refreshTokenOptions);

            this.logger.LogInformation(
            "JWT cookies set for OAuth callback - Access token expires: {Expires}, Refresh token expires: {RefreshExpires}",
            cookieOptions.Expires,
            refreshTokenOptions.Expires);

            // Validate and construct secure callback URL
            string? baseUrl = this.frontendSettings.BaseUrl;
            if (string.IsNullOrEmpty(baseUrl))
            {
                this.logger.LogWarning("Invalid frontend BaseUrl: {BaseUrl}", "null");
                return this.BadRequest("Invalid callback configuration");
            }

            if (!UriHelper.TryCreateAbsoluteUri(baseUrl, out var validatedBaseUri) || validatedBaseUri == null)
            {
                this.logger.LogWarning("Invalid frontend BaseUrl: {BaseUrl}", baseUrl);
                return this.BadRequest("Invalid callback configuration");
            }

            var isLocal = this.HttpContext.Connection.RemoteIpAddress?.ToString() == "127.0.0.1" ||
                         this.HttpContext.Connection.RemoteIpAddress?.ToString() == "::1";
            if (validatedBaseUri.Scheme != "https" && !isLocal)
            {
                this.logger.LogWarning("Insecure frontend BaseUrl scheme for non-local request: {BaseUrl}", baseUrl);
                return this.BadRequest("HTTPS required for non-local requests");
            }

            var callbackUrl = $"{baseUrl}oauth/callback?auth=success&token={Uri.EscapeDataString(response.Token)}&provider={provider}";
            if (!string.IsNullOrEmpty(response.RefreshToken))
            {
                callbackUrl += $"&refresh_token={Uri.EscapeDataString(response.RefreshToken)}";
            }
            return this.Redirect(callbackUrl);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing OAuth callback for provider: {Provider}", provider);
            this.logger.LogInformation("Exception details - Message: {Message}, Type: {Type}", ex.Message, ex.GetType().Name);

            // Handle specific OAuth errors with secure redirects
            if (ex.Message == "user_not_found")
            {
                this.logger.LogWarning("OAuth login failed - user not found, redirecting to login page");
                return this.SafeRedirect("login?error=user_not_found");
            }
            else if (ex.Message == "user_already_exists")
            {
                this.logger.LogWarning("OAuth registration failed - user already exists, redirecting to register page");
                return this.SafeRedirect("register?error=user_already_exists");
            }

            this.logger.LogWarning("OAuth failed with unknown error, redirecting to auth callback page");
            return this.SafeRedirect("auth/callback?error=oauth_failed");
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

            var action = request.Action; // Get action from request body

            this.logger.LogInformation(
                "Telegram OAuth callback received - User ID: {UserId}, State: {State}, Action: {Action}",
                request.UserData?.Id.ToString() ?? "null",
                (request.State ?? "null").Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal),
                (action ?? "null").Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal));

            // Convert Telegram user data to OAuth format
            var telegramCode = JsonSerializer.Serialize(request.UserData);

            this.logger.LogInformation(
                "Telegram OAuth callback received - UserData: {UserData}, SerializedCode: {TelegramCode}",
                JsonSerializer.Serialize(request.UserData)?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                (telegramCode ?? string.Empty)[..Math.Min(telegramCode?.Length ?? 0, 100)] + "...");

            // For Telegram, always generate a temporary state
            // Telegram doesn't return state in callback, so we need to handle this
            var state = $"telegram_temp_{Guid.NewGuid():N}";

            this.logger.LogInformation(
                "Generated temporary state for Telegram OAuth - State: {State}, Action: {Action}, LinkingAction: {LinkingAction}, UserId: {UserId}",
                state,
                action ?? "null",
                request.LinkingAction,
                request.UserId ?? "null");

            var oauthRequest = new OAuthCallbackRequestDto
            {
                Provider = "telegram",
                Code = telegramCode ?? string.Empty,
                State = state,
                Action = action, // Pass action from request body
                LinkingAction = request.LinkingAction, // Pass linking flag from frontend
                UserId = request.UserId ?? string.Empty // Pass userId from frontend for linking validation
            };

            this.logger.LogInformation(
                "Created OAuth request for Telegram - Provider: {Provider}, Action: {Action}, State: {State}, LinkingAction: {LinkingAction}, UserId: {UserId}",
                oauthRequest.Provider.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal),
                (oauthRequest.Action ?? "null").Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal),
                oauthRequest.State.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal),
                oauthRequest.LinkingAction,
                oauthRequest.UserId.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal));

            var response = await this.oauthService.ProcessCallbackAsync(oauthRequest, ipAddress, userAgent);

            this.logger.LogInformation("Telegram OAuth processing result - Success: {Success}, Message: {Message}", response.Success, (response.Message ?? "null").Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal));

            if (!response.Success)
            {
                this.logger.LogWarning(
                    "Telegram OAuth callback failed - Message: {Message}",
                    (response.Message ?? "null").Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal));

                // Return error response for frontend to handle
                return this.Ok(new
                {
                    success = false,
                    message = response.Message,
                    token = string.Empty,
                    refreshToken = string.Empty,
                    user = (object?)null
                });
            }

            this.logger.LogInformation(
                "Telegram OAuth callback processed successfully - User: {UserId}, Token: {Token}",
                response.User?.Id,
                response.Token.Length > 20 ? $"{response.Token[..20]}..." : response.Token);

            // Return tokens in response body for frontend to handle
            // Frontend will redirect with tokens
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
            this.logger.LogInformation("Exception details - Message: {Message}, Type: {Type}", ex.Message, ex.GetType().Name);

            // Handle specific OAuth errors
            if (ex.Message == "user_not_found")
            {
                this.logger.LogWarning("Telegram OAuth login failed - user not found");
                return this.Ok(new { success = false, message = "user_not_found" });
            }
            else if (ex.Message == "user_already_exists")
            {
                this.logger.LogWarning("Telegram OAuth registration failed - user already exists");
                return this.Ok(new { success = false, message = "user_already_exists" });
            }

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

    private static (bool IsLinking, string Provider, string UserId, string ActualState) ParseLinkingState(string state)
    {
        if (string.IsNullOrEmpty(state) || !state.StartsWith("linking_", StringComparison.OrdinalIgnoreCase))
        {
            return (false, string.Empty, string.Empty, state);
        }

        // Format: linking_{provider}_{userId}_{actualState}
        var parts = state.Split('_', 4);
        if (parts.Length < 4)
        {
            // Fallback format: linking_{provider}_{userId}
            if (parts.Length >= 3)
            {
                return (true, parts[1], parts[2], string.Empty);
            }
            return (false, string.Empty, string.Empty, state);
        }

        return (true, parts[1], parts[2], parts[3]);
    }

    private static string ExtractStateFromState(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            return state;
        }

        // Only extract from provider-prefixed state (google_, github_, telegram_)
        // Don't extract from regular state or linking state
        if (state.StartsWith("google_", StringComparison.OrdinalIgnoreCase) ||
            state.StartsWith("github_", StringComparison.OrdinalIgnoreCase) ||
            state.StartsWith("telegram_", StringComparison.OrdinalIgnoreCase))
        {
            var underscoreIndex = state.IndexOf('_', StringComparison.Ordinal);
            if (underscoreIndex > 0 && underscoreIndex < state.Length - 1)
            {
                return state[(underscoreIndex + 1)..];
            }
        }

        return state; // Return as-is for regular state and linking state
    }

    /// <summary>
    /// Performs secure redirect with URL validation.
    /// </summary>
    private ActionResult SafeRedirect(string path)
    {
        string? baseUrl = this.frontendSettings.BaseUrl;
        if (string.IsNullOrEmpty(baseUrl))
        {
            this.logger.LogWarning("Invalid frontend BaseUrl for redirect: {BaseUrl}", "null");
            return this.BadRequest("Invalid redirect configuration");
        }

        if (!UriHelper.TryCreateAbsoluteUri(baseUrl, out var validatedBaseUri) || validatedBaseUri == null)
        {
            this.logger.LogWarning("Invalid frontend BaseUrl for redirect: {BaseUrl}", baseUrl);
            return this.BadRequest("Invalid redirect configuration");
        }

        var isLocal = this.HttpContext.Connection.RemoteIpAddress?.ToString() == "127.0.0.1" ||
                     this.HttpContext.Connection.RemoteIpAddress?.ToString() == "::1";
        if (validatedBaseUri.Scheme != "https" && !isLocal)
        {
            this.logger.LogWarning("Insecure frontend BaseUrl for redirect: {BaseUrl}", baseUrl);
            return this.BadRequest("HTTPS required for redirects");
        }

        var fullPath = $"{baseUrl}{path}";
        if (!UriHelper.TryCreateAbsoluteUri(fullPath, out var fullUri) || fullUri == null)
        {
            this.logger.LogWarning("Invalid redirect URL constructed: {Url}", fullPath);
            return this.BadRequest("Invalid redirect URL");
        }

        // Ensure the redirect is to the same base domain
        if (fullUri.Host != validatedBaseUri.Host || fullUri.Scheme != validatedBaseUri.Scheme)
        {
            this.logger.LogWarning("Redirect URL points to different domain: {Url}", fullPath);
            return this.BadRequest("Cross-domain redirects not allowed");
        }

        return this.Redirect(fullPath);
    }

    #endregion
}
