namespace ServerEye.Core.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Helpers;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Interfaces.Services.Billing;
using ServerEye.Core.Services.OAuth;
using ServerEye.Core.Services.OAuth.Factory;

public sealed class OAuthService(
    IUserRepository userRepository,
    IUserExternalLoginRepository externalLoginRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtService jwtService,
    IOAuthProviderFactory providerFactory,
    IDistributedCache cache,
    ISubscriptionService subscriptionService,
    ILogger<OAuthService> logger,
    OAuthMetrics metrics) : IOAuthService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IUserExternalLoginRepository externalLoginRepository = externalLoginRepository;
    private readonly IRefreshTokenRepository refreshTokenRepository = refreshTokenRepository;
    private readonly IJwtService jwtService = jwtService;
    private readonly IOAuthProviderFactory providerFactory = providerFactory;
    private readonly IDistributedCache cache = cache;
    private readonly ISubscriptionService subscriptionService = subscriptionService;
    private readonly ILogger<OAuthService> logger = logger;
    private readonly OAuthMetrics metrics = metrics;

    // Public interface implementation
    public async Task<OAuthChallengeResponseDto> CreateChallengeAsync(OAuthProvider provider, Uri? returnUrl = null, string? action = null, CancellationToken cancellationToken = default)
    {
        using var activity = OAuthActivitySource.StartCreateChallengeActivity(provider.ToString(), action, returnUrl);
        var startTime = DateTime.UtcNow;

        this.logger.LogInformation("CreateChallengeAsync called - Provider: {Provider}, ReturnUrl: {ReturnUrl}, Action: {Action}", provider, (returnUrl?.ToString() ?? "null").Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal), (action ?? "null").Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal));

        try
        {
            if (!this.IsProviderEnabled(provider))
            {
                this.logger.LogWarning("OAuth provider {Provider} is not enabled", provider);
                this.metrics.RecordError(provider.ToString(), "create_challenge", "provider_disabled");
                activity?.SetError("provider_disabled", $"OAuth provider {provider} is not enabled");
                throw new InvalidOperationException($"OAuth provider {provider} is not enabled");
            }

            var providerInstance = providerFactory.GetProvider(provider);

            var state = GenerateSecureRandomString(32);

            // Embed action in state if provided: "action_randomstate"
            var stateWithAction = state;
            if (!string.IsNullOrEmpty(action))
            {
                stateWithAction = $"{action}_{state}";
                this.logger.LogInformation("Embedded action in state - Action: {Action}, OriginalState: {State}, StateWithAction: {StateWithAction}", action.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal), state.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal), stateWithAction.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal));
            }

            var codeVerifier = GenerateSecureRandomString(128);
            var codeChallenge = Base64UrlEncode(SHA256Hash(codeVerifier));

            this.logger.LogInformation(
                "Generated OAuth parameters - State: {State}, CodeVerifier: {CodeVerifier}, CodeChallenge: {CodeChallenge}",
                (state ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal),
                (codeVerifier ?? string.Empty)[..Math.Min(codeVerifier?.Length ?? 0, 20)] + "...",
                (codeChallenge ?? string.Empty)[..Math.Min(codeChallenge?.Length ?? 0, 20)] + "...");

            // Store code verifier with the state that will actually be returned by provider
            // For GitHub, this includes the provider prefix; for Google, it doesn't
            await cache.SetStringAsync(
                $"oauth:code_verifier:{stateWithAction}",
                codeVerifier ?? string.Empty,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // 10 minutes expiry
                },
                cancellationToken);

            this.logger.LogInformation("Stored code verifier for OAuth challenge - State: {State}", state);

            var challengeResponse = await providerInstance.CreateChallengeAsync(stateWithAction, codeChallenge ?? string.Empty, returnUrl);

            this.logger.LogInformation("Created challenge URL for provider {Provider}: {ChallengeUrl}", provider, (challengeResponse.ChallengeUrl?.ToString() ?? string.Empty)[..Math.Min(challengeResponse.ChallengeUrl?.ToString()?.Length ?? 0, 100)] + "...");

            // Record metrics
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            this.metrics.RecordChallengeCreated(provider.ToString(), action);
            this.metrics.RecordChallengeCreationDuration(provider.ToString(), duration, action);

            activity?.SetTag(OAuthActivitySource.StateAttribute, stateWithAction);
            activity?.SetTag(OAuthActivitySource.CodeVerifierAttribute, (codeVerifier ?? string.Empty)[..Math.Min(codeVerifier?.Length ?? 0, 10)] + "...");
            activity?.SetSuccess();

            return new OAuthChallengeResponseDto
            {
                ChallengeUrl = challengeResponse.ChallengeUrl ?? new Uri(string.Empty),
                State = stateWithAction, // Return state with action prefix
                CodeVerifier = codeVerifier ?? string.Empty,
                Action = action
            };
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            this.metrics.RecordError(provider.ToString(), "create_challenge", ex.GetType().Name, ex.Message);
            activity?.SetError(ex.GetType().Name, ex.Message, ex);
            this.logger.LogError(ex, "Error creating OAuth challenge for provider {Provider}", provider);
            throw;
        }
    }

    public async Task<AuthResponseDto> ProcessCallbackAsync(OAuthCallbackRequestDto request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        using var activity = OAuthActivitySource.StartProcessCallbackActivity(request.Provider, ipAddress, userAgent);
        var startTime = DateTime.UtcNow;

        var provider = this.ParseProvider(request.Provider);
        this.logger.LogInformation("ProcessCallbackAsync START - Provider: {Provider}", provider);

        // Extract action from request or state
        var action = request.Action;
        var originalState = request.State;

        // Special handling for Telegram OAuth
        bool isTelegramTemp = originalState.StartsWith("telegram_temp_", StringComparison.OrdinalIgnoreCase);

        // For Telegram, try to extract action from sessionStorage or use auto mode
        if (provider == OAuthProvider.Telegram && isTelegramTemp)
        {
            this.logger.LogInformation("Telegram OAuth detected with temporary state, using action from request parameter");

            // For Telegram, we rely on the action parameter from the query string
            // If not provided, default to auto mode for backward compatibility
            if (string.IsNullOrEmpty(action))
            {
                this.logger.LogWarning("Telegram OAuth without action parameter, defaulting to auto mode");
                action = "auto";
            }
        }

        // For other providers, extract action from state format: "provider_action_randomstate"
        else if (string.IsNullOrEmpty(action) && !string.IsNullOrEmpty(request.State))
        {
            var stateParts = request.State.Split('_', 3); // Split max into 3 parts: provider, action, randomstate
            if (stateParts.Length >= 3 && (stateParts[1] == "login" || stateParts[1] == "register"))
            {
                // Format: provider_action_randomstate
                action = stateParts[1]; // Action is the second part
                originalState = stateParts[2]; // The third part is the actual state
                this.logger.LogInformation("Extracted action from provider state - Provider: {Provider}, Action: {Action}, OriginalState: {OriginalState}", LogSanitizer.Sanitize(stateParts[0]), LogSanitizer.Sanitize(action), LogSanitizer.Sanitize(originalState));
            }
            else if (stateParts.Length >= 2 && (stateParts[0] == "login" || stateParts[0] == "register"))
            {
                // Fallback for format: action_randomstate (without provider prefix)
                action = stateParts[0];
                originalState = stateParts[1];
                this.logger.LogInformation("Extracted action from simple state - Action: {Action}, OriginalState: {OriginalState}", LogSanitizer.Sanitize(action), LogSanitizer.Sanitize(originalState));
            }
        }

        this.logger.LogInformation("ProcessCallbackAsync - Provider: {Provider}, Action: {Action}, State: {State}, OriginalState: {OriginalState}", provider, LogSanitizer.Sanitize(action) ?? "auto", LogSanitizer.Sanitize(request.State), LogSanitizer.Sanitize(originalState));

        try
        {
            // Retrieve code verifier from memory
            // For Telegram temporary states, we don't need code verifier
            string? codeVerifier = null;

            if (!isTelegramTemp)
            {
                codeVerifier = await cache.GetStringAsync($"oauth:code_verifier:{request.State}", cancellationToken);

                if (codeVerifier == null)
                {
                    this.logger.LogError("Code verifier not found for state: {State}", LogSanitizer.Sanitize(request.State));
                    this.metrics.RecordError(provider.ToString(), "process_callback", "code_verifier_not_found");
                    activity?.SetError("code_verifier_not_found", "Invalid or expired OAuth state");
                    throw new InvalidOperationException("Invalid or expired OAuth state");
                }

                this.logger.LogInformation("Retrieved code verifier for OAuth callback - State: {State}", LogSanitizer.Sanitize(request.State));

                // Remove code verifier from Redis
                await cache.RemoveAsync($"oauth:code_verifier:{request.State}", cancellationToken);
            }
            else
            {
                this.logger.LogInformation("Using temporary state for Telegram OAuth - State: {State}", LogSanitizer.Sanitize(originalState));
            }

            // Get provider instance and exchange code for token
            var providerInstance = providerFactory.GetProvider(provider);
            var exchangeStartTime = DateTime.UtcNow;
            var tokenResponse = await providerInstance.ExchangeCodeAsync(request.Code, codeVerifier ?? string.Empty, cancellationToken);
            var exchangeDuration = (DateTime.UtcNow - exchangeStartTime).TotalSeconds;

            // Record token exchange metrics
            this.metrics.RecordTokenExchange(provider.ToString(), true);
            this.metrics.RecordTokenExchangeDuration(provider.ToString(), exchangeDuration, true);

            // Get user info from provider
            var userInfoStartTime = DateTime.UtcNow;
            var userInfo = await providerInstance.GetUserInfoAsync(tokenResponse.AccessToken, tokenResponse.IdToken, cancellationToken);
            var userInfoDuration = (DateTime.UtcNow - userInfoStartTime).TotalSeconds;

            // Record user info metrics
            this.metrics.RecordUserInfoRequest(provider.ToString(), true);
            this.metrics.RecordUserInfoRetrievalDuration(provider.ToString(), userInfoDuration, true);

            // Check if external login already exists
            var existingExternalLogin = await this.externalLoginRepository.GetByProviderAndProviderUserIdAsync(provider, userInfo.Id, cancellationToken);
            User? user = null;

            this.logger.LogInformation(
                "OAuth callback - Provider: {Provider}, ProviderUserId: {ProviderUserId}, ExternalLoginFound: {ExternalLoginFound}, LinkingAction: {LinkingAction}, RequestUserId: {RequestUserId}",
                provider,
                LogSanitizer.Sanitize(userInfo.Id),
                existingExternalLogin != null,
                request.LinkingAction,
                LogSanitizer.Sanitize(request.UserId ?? "null"));

            activity?.SetTag(OAuthActivitySource.ExternalIdAttribute, userInfo.Id);
            activity?.SetTag(OAuthActivitySource.EmailAttribute, userInfo.Email);

            if (existingExternalLogin != null)
            {
                // User with this external login already exists
                user = await this.userRepository.GetByIdAsync(existingExternalLogin.UserId);
                this.logger.LogInformation("Found existing user via external login - UserId: {UserId}", user?.Id);

                // CRITICAL: Check if this is a linking attempt to a DIFFERENT user account
                // Only check if LinkingAction is explicitly TRUE (not just UserId present)
                if (request.LinkingAction && !string.IsNullOrEmpty(request.UserId))
                {
                    // Parse the requesting user ID
                    Guid? requestingUserId = null;
                    if (Guid.TryParse(request.UserId, out var parsedUserId))
                    {
                        requestingUserId = parsedUserId;
                    }

                    // If the provider is already linked to a DIFFERENT user, this is an error
                    if (requestingUserId.HasValue && existingExternalLogin.UserId != requestingUserId.Value)
                    {
                        this.logger.LogWarning(
                            "OAuth linking attempt failed - Provider {Provider} is already linked to user {ExistingUserId}, but user {RequestingUserId} is trying to link it",
                            provider,
                            existingExternalLogin.UserId,
                            requestingUserId.Value);
                        throw new InvalidOperationException("This external account is already linked to another user");
                    }

                    this.logger.LogInformation(
                        "OAuth linking detected but provider is already linked to the same user {UserId} - proceeding with login",
                        existingExternalLogin.UserId);
                }
            }

            // Apply action-based logic
            this.logger.LogInformation("Applying action-based logic - Action: {Action}, UserExists: {UserExists}", (action ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal), user != null);

            if (!string.IsNullOrEmpty(action))
            {
                if (action.Equals("login", StringComparison.OrdinalIgnoreCase))
                {
                    // LOGIN mode: only authenticate existing users
                    if (user == null)
                    {
                        this.logger.LogWarning("OAuth login failed - user not found for provider {Provider}", provider);
                        throw new InvalidOperationException("user_not_found");
                    }

                    this.logger.LogInformation("OAuth login successful for existing user {UserId}", user.Id);
                }
                else if (action.Equals("register", StringComparison.OrdinalIgnoreCase))
                {
                    // REGISTER mode: only register new users
                    if (user != null)
                    {
                        this.logger.LogWarning("OAuth registration failed - user already exists for provider {Provider}", provider);
                        throw new InvalidOperationException("user_already_exists");
                    }

                    // Create new user
                    user = await this.FindOrCreateUserAsync(provider, userInfo, cancellationToken);
                    this.logger.LogInformation("OAuth registration successful for new user {UserId}", user.Id);
                }
                else if (action.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    // AUTO mode (backward compatibility): find or create user
                    user = await this.FindOrCreateUserAsync(provider, userInfo, cancellationToken);
                    this.logger.LogInformation("OAuth auto mode - user {UserId} authenticated", user.Id);
                }
            }
            else
            {
                // AUTO mode (backward compatibility): find or create user
                user = await this.FindOrCreateUserAsync(provider, userInfo, cancellationToken);
                this.logger.LogInformation("OAuth auto mode - user {UserId} authenticated", user.Id);
            }

            if (user == null)
            {
                throw new InvalidOperationException("Failed to authenticate or create user");
            }

            // Link external login if needed
            if (existingExternalLogin == null)
            {
                await this.LinkExternalLoginAsync(user.Id, provider, userInfo, cancellationToken);
            }
            else
            {
                // Update last used timestamp for existing login
                existingExternalLogin.LastUsedAt = DateTime.UtcNow;
                await this.externalLoginRepository.UpdateAsync(existingExternalLogin, cancellationToken);
            }

            // Generate JWT tokens
            var token = this.jwtService.GenerateAccessToken(user);
            var refreshToken = this.jwtService.GenerateRefreshToken(user);

            // Save refresh token to database
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };
            await this.refreshTokenRepository.AddAsync(refreshTokenEntity);

            this.logger.LogInformation("User {UserId} authenticated via OAuth provider {Provider} with action {Action}", user.Id, provider, LogSanitizer.Sanitize(action) ?? "auto");

            // Record success metrics
            var totalDuration = (DateTime.UtcNow - startTime).TotalSeconds;
            activity?.SetTag(OAuthActivitySource.UserIdAttribute, user.Id.ToString());
            activity?.SetSuccess();

            var requiresEmailVerification = user.HasPassword && !user.IsEmailVerified && !string.IsNullOrEmpty(user.Email);
            this.logger.LogInformation(
                "OAuth response - UserId: {UserId}, HasPassword: {HasPassword}, IsEmailVerified: {IsEmailVerified}, RequiresEmailVerification: {RequiresEmailVerification}",
                user.Id,
                user.HasPassword,
                user.IsEmailVerified,
                requiresEmailVerification);

            return new AuthResponseDto
            {
                User = new AuthUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email ?? string.Empty,
                    ServerId = user.ServerId,
                    IsEmailVerified = user.IsEmailVerified,
                    RequiresEmailVerification = requiresEmailVerification
                },
                Token = token,
                RefreshToken = refreshToken,
                ExpiresIn = 3600
            };
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            this.metrics.RecordError(provider.ToString(), "process_callback", ex.GetType().Name, ex.Message);
            activity?.SetError(ex.GetType().Name, ex.Message, ex);
            this.logger.LogError(ex, "Error processing OAuth callback for provider {Provider}", provider);
            throw;
        }
    }

    public async Task<List<OAuthProviderInfoDto>> GetUserExternalLoginsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var externalLogins = await this.externalLoginRepository.GetByUserIdAsync(userId, cancellationToken);

        return externalLogins.Select(login => new OAuthProviderInfoDto
        {
            Provider = login.Provider,
            ProviderUserId = login.ProviderUserId,
            ProviderEmail = login.ProviderEmail,
            ProviderUsername = login.ProviderUsername,
            ProviderAvatarUrl = login.ProviderAvatarUrl,
            CreatedAt = login.CreatedAt,
            LastUsedAt = login.LastUsedAt
        }).ToList();
    }

    public async Task<AuthResponseDto> LinkExternalLoginAsync(Guid userId, OAuthLinkRequestDto request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var provider = this.ParseProvider(request.Provider);

        // Check if already linked
        var existingLogin = await this.externalLoginRepository.GetByUserIdAndProviderAsync(userId, provider, cancellationToken);
        if (existingLogin != null)
        {
            throw new InvalidOperationException($"Account is already linked to {provider}");
        }

        // Exchange code for token
        // For linking external login, we need to retrieve the original code verifier
        var originalState = request.State; // This should be the actualState from linking state

        var storedCodeVerifier = await cache.GetStringAsync($"oauth:code_verifier:{originalState}", cancellationToken);

        if (storedCodeVerifier == null)
        {
            this.logger.LogError("Code verifier not found for linking state: {State}", LogSanitizer.Sanitize(originalState));
            throw new InvalidOperationException("Invalid or expired OAuth state for linking");
        }

        this.logger.LogInformation("Retrieved code verifier for OAuth linking - State: {State}", LogSanitizer.Sanitize(originalState));

        // Remove code verifier from Redis
        await cache.RemoveAsync($"oauth:code_verifier:{originalState}", cancellationToken);

        var providerInstance = providerFactory.GetProvider(provider);
        var tokenResponse = await providerInstance.ExchangeCodeAsync(request.Code, storedCodeVerifier, cancellationToken);

        // Get user info
        var userInfo = await providerInstance.GetUserInfoAsync(tokenResponse.AccessToken, tokenResponse.IdToken, cancellationToken);

        // Check if this external login is already linked to another account
        var otherUserLogin = await this.externalLoginRepository.GetByProviderAndProviderUserIdAsync(provider, userInfo.Id, cancellationToken);
        if (otherUserLogin != null && otherUserLogin.UserId != userId)
        {
            throw new InvalidOperationException("This external account is already linked to another user");
        }

        // Link the external login
        await this.LinkExternalLoginAsync(userId, provider, userInfo, cancellationToken);

        // Generate new tokens
        var user = await this.userRepository.GetByIdAsync(userId)
               ?? throw new InvalidOperationException("User not found");
        var token = this.jwtService.GenerateAccessToken(user);
        var refreshToken = this.jwtService.GenerateRefreshToken(user);

        // Save refresh token to database
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
        await this.refreshTokenRepository.AddAsync(refreshTokenEntity);

        return new AuthResponseDto
        {
            User = new AuthUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email ?? string.Empty,
                ServerId = user.ServerId,
                IsEmailVerified = user.IsEmailVerified,
                RequiresEmailVerification = user.HasPassword && !user.IsEmailVerified && !string.IsNullOrEmpty(user.Email)
            },
            Token = token,
            RefreshToken = refreshToken,
            ExpiresIn = 3600
        };
    }

    public async Task UnlinkExternalLoginAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default)
    {
        var externalLogin = await this.externalLoginRepository.GetByUserIdAndProviderAsync(userId, provider, cancellationToken)
                          ?? throw new InvalidOperationException("External login not found");

        // Check if user will lose access to account
        var user = await this.userRepository.GetByIdAsync(userId)
               ?? throw new InvalidOperationException("User not found");
        if (!user.HasPassword)
        {
            var otherLogins = await this.externalLoginRepository.GetByUserIdAsync(userId, cancellationToken);
            if (otherLogins.Count <= 1)
            {
                throw new InvalidOperationException("Cannot unlink the only authentication method. Please set a password first.");
            }
        }

        await this.externalLoginRepository.DeleteAsync(externalLogin, cancellationToken);
    }

    public async Task<OAuthUserInfoDto> GetUserInfoAsync(OAuthProvider provider, string accessToken, string? idToken = null, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation(
            "GetUserInfoAsync called - Provider: {Provider}, AccessToken: {AccessToken}, IdToken: {IdToken}",
            provider,
            accessToken[..Math.Min(accessToken.Length, 20)] + "...",
            string.IsNullOrEmpty(idToken) ? "NULL" : idToken[..Math.Min(idToken.Length, 20)] + "...");

        var providerInstance = providerFactory.GetProvider(provider);
        return await providerInstance.GetUserInfoAsync(accessToken, idToken, cancellationToken);
    }

    public async Task<bool> ValidateAccessTokenAsync(OAuthProvider provider, string accessToken, CancellationToken cancellationToken = default)
    {
        var providerInstance = providerFactory.GetProvider(provider);
        return await providerInstance.ValidateTokenAsync(accessToken, cancellationToken);
    }

    public bool IsProviderEnabled(OAuthProvider provider)
    {
        return providerFactory.IsProviderEnabled(provider);
    }

    public OAuthProvider ParseProvider(string providerName)
    {
        return providerName.ToUpperInvariant() switch
        {
            "GOOGLE" => OAuthProvider.Google,
            "GITHUB" => OAuthProvider.GitHub,
            "TELEGRAM" => OAuthProvider.Telegram,
            _ => throw new ArgumentException($"Unknown provider: {providerName}")
        };
    }

    public async Task<bool> CanLinkAccountAsync(Guid userId, OAuthProvider provider, string providerUserId, CancellationToken cancellationToken = default)
    {
        var existingLogin = await this.externalLoginRepository.GetByProviderAndProviderUserIdAsync(provider, providerUserId, cancellationToken);
        return existingLogin != null && existingLogin.UserId != userId;
    }

    public async Task<User?> FindUserByExternalLoginAsync(OAuthProvider provider, string providerUserId, CancellationToken cancellationToken = default)
    {
        var externalLogin = await this.externalLoginRepository.GetByProviderAndProviderUserIdAsync(provider, providerUserId, cancellationToken);
        if (externalLogin == null)
        {
            return null;
        }

        return await this.userRepository.GetByIdAsync(externalLogin.UserId);
    }

    public async Task<User> CreateOrUpdateUserFromExternalLoginAsync(OAuthProvider provider, OAuthUserInfoDto userInfo, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        // Check if user with this external login already exists
        var existingLogin = await this.externalLoginRepository.GetByProviderAndProviderUserIdAsync(provider, userInfo.Id, cancellationToken);
        if (existingLogin != null)
        {
            // Update existing external login info
            existingLogin.ProviderEmail = userInfo.Email ?? string.Empty; // Keep empty string for ProviderEmail
            existingLogin.ProviderUsername = userInfo.Username ?? string.Empty;
            existingLogin.ProviderAvatarUrl = userInfo.AvatarUrl;
            existingLogin.ProviderData = JsonSerializer.Serialize(userInfo.RawData ?? new Dictionary<string, object>());
            existingLogin.LastUsedAt = DateTime.UtcNow;

            await this.externalLoginRepository.UpdateAsync(existingLogin, cancellationToken);

            return await this.userRepository.GetByIdAsync(existingLogin.UserId)
                   ?? throw new InvalidOperationException("User not found");
        }

        // Check if user with same email exists
        User? user = null;
        if (!string.IsNullOrEmpty(userInfo.Email))
        {
            user = await this.userRepository.GetByEmailAsync(userInfo.Email);
        }

        if (user == null)
        {
            // Create new user
            // OAuth providers verify email, so if email is provided, it's verified
            var isEmailVerified = !string.IsNullOrEmpty(userInfo.Email) || userInfo.EmailVerified;

            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = !string.IsNullOrEmpty(userInfo.Username) ? userInfo.Username :
                          !string.IsNullOrEmpty(userInfo.Email) ? userInfo.Email.Split('@')[0] :
                          $"oauth_{userInfo.Id}",
                Email = userInfo.Email, // Can be null for providers like Telegram
                Role = UserRole.User,
                IsEmailVerified = isEmailVerified,
                EmailVerifiedAt = isEmailVerified ? DateTime.UtcNow : null,
                Password = string.Empty, // OAuth users don't have passwords
                HasPassword = false,
                ServerId = Guid.NewGuid()
            };

            await this.userRepository.AddAsync(user);
        }

        // Link external login
        await this.LinkExternalLoginAsync(user.Id, provider, userInfo, cancellationToken);

        return user;
    }

    // Static helper methods
    private static string GenerateSecureRandomString(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("=", string.Empty, StringComparison.Ordinal)[..length];
    }

    private static byte[] SHA256Hash(string input)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(input));
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("=", string.Empty, StringComparison.Ordinal);
    }

    private static string? SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }

    private async Task<User> FindOrCreateUserAsync(OAuthProvider provider, OAuthUserInfoDto userInfo, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("FindOrCreateUserAsync - Provider: {Provider}, UserId: {UserId}", provider, LogSanitizer.Sanitize(userInfo.Id));

        // Check if external login already exists
        var externalLogin = await this.externalLoginRepository.GetByProviderAndProviderUserIdAsync(provider, userInfo.Id, cancellationToken);
        if (externalLogin != null)
        {
            this.logger.LogInformation("Found existing external login - UserId: {UserId}", externalLogin.UserId);
            return await this.userRepository.GetByIdAsync(externalLogin.UserId)
                   ?? throw new InvalidOperationException("User not found");
        }

        this.logger.LogInformation("External login not found, checking for existing user by email");

        // Check if user with same email exists
        if (!string.IsNullOrEmpty(userInfo.Email))
        {
            var existingUser = await this.userRepository.GetByEmailAsync(userInfo.Email);
            if (existingUser != null)
            {
                this.logger.LogInformation("Found existing user by email - UserId: {UserId}", existingUser.Id);
                return existingUser;
            }
        }

        this.logger.LogInformation("Creating new user for OAuth - Username: {Username}", userInfo.Username);

        // Create new user
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = !string.IsNullOrEmpty(userInfo.Username) ? userInfo.Username :
                      !string.IsNullOrEmpty(userInfo.Email) ? userInfo.Email.Split('@')[0] :
                      $"oauth_{userInfo.Id}",
            Email = userInfo.Email, // Keep null for OAuth users without email
            Role = UserRole.User,
            IsEmailVerified = true, // OAuth users are always considered verified
            EmailVerifiedAt = DateTime.UtcNow, // Set verification time for OAuth users
            Password = string.Empty, // OAuth users don't have passwords
            HasPassword = false,
            ServerId = Guid.NewGuid()
        };

        await this.userRepository.AddAsync(newUser);
        this.logger.LogInformation("Created new user - UserId: {UserId}", newUser.Id);

        // Create free subscription for new user
        try
        {
            await this.subscriptionService.CreateFreeSubscriptionAsync(newUser.Id);
            this.logger.LogInformation("Created free subscription for new user - UserId: {UserId}", newUser.Id);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to create free subscription for user - UserId: {UserId}", newUser.Id);

            // Don't fail the OAuth flow if subscription creation fails
        }

        return newUser;
    }

    private async Task LinkExternalLoginAsync(Guid userId, OAuthProvider provider, OAuthUserInfoDto userInfo, CancellationToken cancellationToken)
    {
        var externalLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderUserId = userInfo.Id,
            ProviderEmail = userInfo.Email ?? string.Empty,
            ProviderUsername = userInfo.Username ?? string.Empty,
            ProviderAvatarUrl = userInfo.AvatarUrl,
            ProviderData = JsonSerializer.Serialize(userInfo.RawData ?? new Dictionary<string, object>()),
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };

        await this.externalLoginRepository.AddAsync(externalLogin, cancellationToken);
    }
}
