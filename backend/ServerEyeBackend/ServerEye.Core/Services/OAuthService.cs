namespace ServerEye.Core.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public sealed class OAuthService(
    IConfiguration configuration,
    IUserRepository userRepository,
    IUserExternalLoginRepository externalLoginRepository,
    IJwtService jwtService,
    ILogger<OAuthService> logger) : IOAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = null
    };

    // Simple in-memory storage for code verifiers (in production, use Redis or database)
    private static readonly Dictionary<string, string> CodeVerifiers = new();

    private readonly OAuthSettings oauthSettings = configuration.GetSection("OAuth").Get<OAuthSettings>() ?? new OAuthSettings();
    private readonly IUserRepository userRepository = userRepository;
    private readonly IUserExternalLoginRepository externalLoginRepository = externalLoginRepository;
    private readonly IJwtService jwtService = jwtService;
    private readonly ILogger<OAuthService> logger = logger;

    // Public interface implementation
    public async Task<OAuthChallengeResponseDto> CreateChallengeAsync(OAuthProvider provider, Uri? returnUrl = null, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("CreateChallengeAsync called - Provider: {Provider}, ReturnUrl: {ReturnUrl}", provider, returnUrl?.ToString() ?? "null");

        if (!this.IsProviderEnabled(provider))
        {
            this.logger.LogWarning("OAuth provider {Provider} is not enabled", provider);
            throw new InvalidOperationException($"OAuth provider {provider} is not enabled");
        }

        var state = GenerateSecureRandomString(32);
        var codeVerifier = GenerateSecureRandomString(128);
        var codeChallenge = Base64UrlEncode(SHA256Hash(codeVerifier));

        this.logger.LogInformation(
            "Generated OAuth parameters - State: {State}, CodeVerifier: {CodeVerifier}, CodeChallenge: {CodeChallenge}",
            state,
            codeVerifier[..Math.Min(codeVerifier.Length, 20)] + "...",
            codeChallenge[..Math.Min(codeChallenge.Length, 20)] + "...");

        // Store code verifier in memory for later verification
        CodeVerifiers[state] = codeVerifier;

        this.logger.LogInformation("Stored code verifier for OAuth challenge - State: {State}", state);

        var challengeUrl = provider switch
        {
            OAuthProvider.None => throw new ArgumentException("None provider is not valid for challenges"),
            OAuthProvider.Google => this.CreateGoogleChallengeUrl(state, codeChallenge, returnUrl),
            OAuthProvider.GitHub => this.CreateGitHubChallengeUrl(state, codeChallenge, returnUrl),
            OAuthProvider.Telegram => this.CreateTelegramChallengeUrl(state, returnUrl),
            _ => throw new NotSupportedException($"Provider {provider} is not supported")
        };

        this.logger.LogInformation("Created challenge URL for provider {Provider}: {ChallengeUrl}", provider, challengeUrl[..Math.Min(challengeUrl.Length, 100)] + "...");

        return new OAuthChallengeResponseDto
        {
            ChallengeUrl = new Uri(challengeUrl),
            State = state, // Return original state without prefix
            CodeVerifier = codeVerifier
        };
    }

    public async Task<AuthResponseDto> ProcessCallbackAsync(OAuthCallbackRequestDto request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var provider = this.ParseProvider(request.Provider);

        // Retrieve code verifier from memory
        // For Telegram temporary states, we don't need code verifier
        string? codeVerifier = null;
        bool isTelegramTemp = request.State.StartsWith("telegram_temp_", StringComparison.OrdinalIgnoreCase);
        
        if (!isTelegramTemp && !CodeVerifiers.TryGetValue(request.State, out codeVerifier))
        {
            this.logger.LogError("Code verifier not found for state: {State}", request.State);
            throw new InvalidOperationException("Invalid or expired OAuth state");
        }

        if (!isTelegramTemp)
        {
            this.logger.LogInformation("Retrieved code verifier for OAuth callback - State: {State}", request.State);

            // Remove code verifier from memory
            CodeVerifiers.Remove(request.State);
        }
        else
        {
            this.logger.LogInformation("Using temporary state for Telegram OAuth - State: {State}", request.State);
        }

        // Exchange authorization code for access token
        var tokenResponse = await this.ExchangeCodeForTokenAsync(provider, request.Code, codeVerifier ?? string.Empty, cancellationToken);

        // Get user info from provider
        var userInfo = await this.GetUserInfoAsync(provider, tokenResponse.AccessToken, tokenResponse.IdToken, cancellationToken);

        // Find or create user
        var user = await this.FindOrCreateUserAsync(provider, userInfo, cancellationToken);

        // Link external login if needed
        var existingExternalLogin = await this.externalLoginRepository.GetByProviderAndProviderUserIdAsync(provider, userInfo.Id, cancellationToken);
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
        var refreshToken = await Task.FromResult(this.jwtService.GenerateRefreshToken(user));

        this.logger.LogInformation("User {UserId} authenticated via OAuth provider {Provider}", user.Id, provider);

        return new AuthResponseDto
        {
            User = new AuthUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email ?? string.Empty,
                ServerId = user.ServerId
            },
            Token = token,
            RefreshToken = refreshToken,
            ExpiresIn = 3600
        };
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
        // For linking external login, we need to generate a temporary code verifier
        var tempCodeVerifier = GenerateSecureRandomString(128);
        var tokenResponse = await this.ExchangeCodeForTokenAsync(provider, request.Code, tempCodeVerifier, cancellationToken);

        // Get user info
        var userInfo = await this.GetUserInfoAsync(provider, tokenResponse.AccessToken, tokenResponse.IdToken, cancellationToken);

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
        var refreshToken = await Task.FromResult(this.jwtService.GenerateRefreshToken(user));

        return new AuthResponseDto
        {
            User = new AuthUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email ?? string.Empty,
                ServerId = user.ServerId
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

        return provider switch
        {
            OAuthProvider.None => throw new ArgumentException("None provider is not valid for getting user info"),
            OAuthProvider.Google => await GetGoogleUserInfoAsync(accessToken, idToken, cancellationToken),
            OAuthProvider.GitHub => await GetGitHubUserInfoAsync(accessToken, cancellationToken),
            OAuthProvider.Telegram => GetTelegramUserInfoAsync(accessToken),
            _ => throw new NotSupportedException($"Provider {provider} is not supported")
        };
    }

    public async Task<bool> ValidateAccessTokenAsync(OAuthProvider provider, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            await this.GetUserInfoAsync(provider, accessToken, cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsProviderEnabled(OAuthProvider provider)
    {
        return provider switch
        {
            OAuthProvider.None => false,
            OAuthProvider.Google => this.oauthSettings.Google.Enabled,
            OAuthProvider.GitHub => this.oauthSettings.GitHub.Enabled,
            OAuthProvider.Telegram => this.oauthSettings.Telegram.Enabled,
            _ => false
        };
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
            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = !string.IsNullOrEmpty(userInfo.Username) ? userInfo.Username :
                          !string.IsNullOrEmpty(userInfo.Email) ? userInfo.Email.Split('@')[0] :
                          $"oauth_{userInfo.Id}",
                Email = userInfo.Email, // Can be null for providers like Telegram
                Role = UserRole.User,
                IsEmailVerified = userInfo.EmailVerified,
                EmailVerifiedAt = userInfo.EmailVerified ? DateTime.UtcNow : null,
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

    private static string Base64UrlDecode(string input)
    {
        var padded = input.Length % 4 == 0 ? input : input.PadRight(input.Length + (4 - (input.Length % 4)), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
    }

    private static async Task<OAuthUserInfoDto> GetGoogleUserInfoAsync(string accessToken, string? idToken, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();

        // If we have an ID token, decode it first
        if (!string.IsNullOrEmpty(idToken))
        {
            // This is a simplified implementation - in production, you should validate the token signature
            var parts = idToken.Split('.');
            if (parts.Length > 1)
            {
                var payload = Base64UrlDecode(parts[1]);
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(payload, JsonOptions);

                if (tokenData != null)
                {
                    return new OAuthUserInfoDto
                    {
                        Id = tokenData.GetValueOrDefault("sub")?.ToString() ?? string.Empty,
                        Email = tokenData.GetValueOrDefault("email")?.ToString() ?? string.Empty,
                        Name = tokenData.GetValueOrDefault("name")?.ToString() ?? string.Empty,
                        Username = tokenData.GetValueOrDefault("given_name")?.ToString() ?? string.Empty,
                        AvatarUrl = !string.IsNullOrEmpty(tokenData.GetValueOrDefault("picture")?.ToString()) ? new Uri(tokenData.GetValueOrDefault("picture")?.ToString()!) : null,
                        EmailVerified = bool.TryParse(tokenData.GetValueOrDefault("email_verified")?.ToString(), out var verified) && verified,
                        RawData = tokenData
                    };
                }
            }
        }

        // Fallback to API call
        var response = await httpClient.GetAsync(new Uri($"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}"), cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, JsonOptions)
               ?? throw new InvalidOperationException("Failed to get user info from Google");

        return new OAuthUserInfoDto
        {
            Id = userData.GetValueOrDefault("id")?.ToString() ?? string.Empty,
            Email = userData.GetValueOrDefault("email")?.ToString() ?? string.Empty,
            Name = userData.GetValueOrDefault("name")?.ToString() ?? string.Empty,
            Username = userData.GetValueOrDefault("given_name")?.ToString() ?? string.Empty,
            AvatarUrl = !string.IsNullOrEmpty(userData.GetValueOrDefault("picture")?.ToString()) ? new Uri(userData.GetValueOrDefault("picture")?.ToString()!) : null,
            EmailVerified = bool.TryParse(userData.GetValueOrDefault("verified_email")?.ToString(), out var emailVerified) && emailVerified,
            RawData = userData
        };
    }

    private static async Task<OAuthUserInfoDto> GetGitHubUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "ServerEye");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"token {accessToken}");

        var response = await httpClient.GetAsync(new Uri("https://api.github.com/user"), cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, JsonOptions)
               ?? throw new InvalidOperationException("Failed to get user info from GitHub");

        // Get user email (GitHub requires separate API call for email)
        var emailResponse = await httpClient.GetAsync(new Uri("https://api.github.com/user/emails"), cancellationToken);
        emailResponse.EnsureSuccessStatusCode();
        var emailContent = await emailResponse.Content.ReadAsStringAsync(cancellationToken);
        var emailData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(emailContent, JsonOptions);

        var primaryEmail = emailData?.FirstOrDefault(e => bool.TryParse(e.GetValueOrDefault("primary")?.ToString(), out var primary) && primary);
        var email = primaryEmail?.GetValueOrDefault("email")?.ToString() ?? string.Empty;
        var emailVerified = bool.TryParse(primaryEmail?.GetValueOrDefault("verified")?.ToString(), out var verified) && verified;

        return new OAuthUserInfoDto
        {
            Id = userData.GetValueOrDefault("id")?.ToString() ?? string.Empty,
            Email = email,
            Name = userData.GetValueOrDefault("name")?.ToString() ?? string.Empty,
            Username = userData.GetValueOrDefault("login")?.ToString() ?? string.Empty,
            AvatarUrl = !string.IsNullOrEmpty(userData.GetValueOrDefault("avatar_url")?.ToString()) ? new Uri(userData.GetValueOrDefault("avatar_url")?.ToString()!) : null,
            EmailVerified = emailVerified,
            RawData = userData
        };
    }

    private static OAuthUserInfoDto GetTelegramUserInfoAsync(string accessToken)
    {
        // Telegram OAuth returns user data as JSON in the "hash" parameter
        // The accessToken here is actually the user data JSON from Telegram
        
        // DEBUG: Log what we receive
        Console.WriteLine($"[DEBUG] Telegram GetTelegramUserInfoAsync received: {accessToken}");
        
        try
        {
            var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(accessToken, JsonOptions)
                   ?? throw new InvalidOperationException("Failed to parse Telegram user data");
            
            // DEBUG: Log parsed data
            Console.WriteLine($"[DEBUG] Parsed Telegram user data: {string.Join(", ", userData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

            // Extract user information from Telegram data
            // Telegram data comes directly, not nested in "user" field
            var userDict = userData; // userData is already a Dictionary<string, object>

            var userId = userDict.GetValueOrDefault("Id")?.ToString() ?? "unknown";
            var name = $"{userDict.GetValueOrDefault("FirstName")?.ToString() ?? string.Empty} {userDict.GetValueOrDefault("LastName")?.ToString() ?? string.Empty}".Trim();
            var username = userDict.GetValueOrDefault("Username")?.ToString() ?? string.Empty;

            return new OAuthUserInfoDto
            {
                Id = userId,
                Email = null, // Telegram doesn't provide email
                Name = name,
                Username = username,
                AvatarUrl = null, // Avatar photos require separate API calls
                EmailVerified = false, // Telegram doesn't provide email verification
                RawData = userData // Keep as Dictionary for storage
            };
        }
        catch (Exception)
        {
            // Ultimate fallback - treat the access token as user ID
            var userData = new Dictionary<string, object>
            {
                ["id"] = accessToken,
                ["first_name"] = "Telegram",
                ["last_name"] = "User",
                ["username"] = "telegram_user"
            };

            return new OAuthUserInfoDto
            {
                Id = accessToken,
                Email = null, // Telegram doesn't provide email
                Name = "Telegram User",
                Username = "telegram_user",
                AvatarUrl = null,
                EmailVerified = false,
                RawData = userData
            };
        }
    }

    private static Task<TokenResponseDto> ExchangeTelegramCodeAsync(string code)
    {
        // Telegram OAuth flow is different - the "code" parameter contains user data
        // Telegram returns user data directly in the callback, not via token exchange
        // We treat the user data as the "access token" for consistency
        return Task.FromResult(new TokenResponseDto
        {
            AccessToken = code, // This contains the Telegram user data JSON
            TokenType = "Bearer",
            ExpiresIn = 3600,
            IdToken = string.Empty, // Telegram doesn't use id_token
            RefreshToken = string.Empty, // Telegram doesn't use refresh tokens
            Scope = "telegram_user"
        });
    }

    // Private helper methods
    private string CreateGoogleChallengeUrl(string state, string codeChallenge, Uri? returnUrl)
    {
        var settings = this.oauthSettings.Google;
        var scopes = "openid email profile";
        var redirectUri = Uri.EscapeDataString(settings.RedirectUri.ToString());

        var url = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                  $"client_id={settings.ClientId}&" +
                  $"redirect_uri={redirectUri}&" +
                  $"response_type=code&" +
                  $"scope={scopes}&" +
                  $"state={state}&" +
                  $"code_challenge={codeChallenge}&" +
                  $"code_challenge_method=S256&" +
                  $"access_type=offline&" + // Request refresh token
                  $"prompt=consent"; // Force consent dialog to get refresh token

        if (returnUrl != null)
        {
            url += $"&return_url={Uri.EscapeDataString(returnUrl.ToString())}";
        }

        return url;
    }

    private string CreateGitHubChallengeUrl(string state, string codeChallenge, Uri? returnUrl)
    {
        this.logger.LogInformation(
            "CreateGitHubChallengeUrl called - State: {State}, CodeChallenge: {CodeChallenge}, ReturnUrl: {ReturnUrl}",
            state,
            codeChallenge[..Math.Min(codeChallenge.Length, 20)] + "...",
            returnUrl?.ToString() ?? "null");

        var settings = this.oauthSettings.GitHub;
        var scopes = "user:email";

        // Add provider prefix to state so we can identify it in callback
        var stateWithProvider = $"github_{state}";
        var redirectUri = Uri.EscapeDataString(settings.RedirectUri.ToString());

        this.logger.LogInformation(
            "GitHub OAuth settings - ClientId: {ClientId}, RedirectUri: {RedirectUri}, Scopes: {Scopes}",
            settings.ClientId,
            settings.RedirectUri.ToString(),
            scopes);

        // GitHub doesn't support PKCE, so we don't include code_challenge parameters
        var url = $"https://github.com/login/oauth/authorize?" +
                  $"client_id={settings.ClientId}&" +
                  $"redirect_uri={redirectUri}&" +
                  $"scope={scopes}&" +
                  $"state={stateWithProvider}";

        if (returnUrl != null)
        {
            url += $"&return_url={Uri.EscapeDataString(returnUrl.ToString())}";
        }

        this.logger.LogInformation("Generated GitHub challenge URL: {Url}", url[..Math.Min(url.Length, 100)] + "...");
        return url;
    }

    private string CreateTelegramChallengeUrl(string state, Uri? returnUrl)
    {
        var settings = this.oauthSettings.Telegram;
        
        // Add provider prefix to state so we can identify it in callback
        var stateWithProvider = $"telegram_{state}";
        var redirectUri = Uri.EscapeDataString(settings.RedirectUri.ToString());

        // Telegram OAuth URL with correct parameters
        var url = $"https://oauth.telegram.org/auth?" +
                  $"bot_id={settings.BotToken}&" + // Use bot_id from configuration
                  $"origin=http://127.0.0.1:5246&" + // Fixed origin without path
                  $"request_access=write&" + // Request write access
                  $"redirect_uri=http://127.0.0.1:5246&" + // Use root domain for index.html
                  $"state={stateWithProvider}";

        if (returnUrl != null)
        {
            url += $"&return_url={Uri.EscapeDataString(returnUrl.ToString())}";
        }

        return url;
    }

    private async Task<User> FindOrCreateUserAsync(OAuthProvider provider, OAuthUserInfoDto userInfo, CancellationToken cancellationToken)
    {
        // Check if external login already exists
        var externalLogin = await this.externalLoginRepository.GetByProviderAndProviderUserIdAsync(provider, userInfo.Id, cancellationToken);
        if (externalLogin != null)
        {
            return await this.userRepository.GetByIdAsync(externalLogin.UserId)
                   ?? throw new InvalidOperationException("User not found");
        }

        // Check if user with same email exists
        if (!string.IsNullOrEmpty(userInfo.Email))
        {
            var existingUser = await this.userRepository.GetByEmailAsync(userInfo.Email);
            if (existingUser != null)
            {
                return existingUser;
            }
        }

        // Create new user
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = !string.IsNullOrEmpty(userInfo.Username) ? userInfo.Username :
                      !string.IsNullOrEmpty(userInfo.Email) ? userInfo.Email.Split('@')[0] :
                      $"oauth_{userInfo.Id}",
            Email = userInfo.Email, // Keep null for OAuth users without email
            Role = UserRole.User,
            IsEmailVerified = userInfo.EmailVerified,
            EmailVerifiedAt = userInfo.EmailVerified ? DateTime.UtcNow : null,
            Password = string.Empty, // OAuth users don't have passwords
            HasPassword = false,
            ServerId = Guid.NewGuid()
        };

        await this.userRepository.AddAsync(newUser);
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

    private async Task<TokenResponseDto> ExchangeCodeForTokenAsync(OAuthProvider provider, string code, string codeVerifier, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();

        return provider switch
        {
            OAuthProvider.None => throw new ArgumentException("None provider is not valid for token exchange"),
            OAuthProvider.Google => await this.ExchangeGoogleCodeAsync(httpClient, code, codeVerifier, cancellationToken),
            OAuthProvider.GitHub => await this.ExchangeGitHubCodeAsync(httpClient, code, codeVerifier, cancellationToken),
            OAuthProvider.Telegram => await ExchangeTelegramCodeAsync(code),
            _ => throw new NotSupportedException($"Provider {provider} is not supported")
        };
    }

    private async Task<TokenResponseDto> ExchangeGoogleCodeAsync(HttpClient httpClient, string code, string codeVerifier, CancellationToken cancellationToken)
    {
        var settings = this.oauthSettings.Google;
        var tokenEndpoint = "https://oauth2.googleapis.com/token";

        this.logger.LogInformation(
            "Exchanging Google code for token - ClientId: {ClientId}, RedirectUri: {RedirectUri}, CodeVerifier: {CodeVerifier}",
            settings.ClientId,
            settings.RedirectUri.ToString(),
            codeVerifier.Length > 20 ? $"{codeVerifier[..20]}..." : codeVerifier);

        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = settings.RedirectUri.ToString(),
            ["code_verifier"] = codeVerifier // Add PKCE code verifier
        };

        this.logger.LogInformation(
            "Google token request parameters: {Parameters}",
            string.Join(
                ", ",
                parameters.Select(p => $"{p.Key}={p.Value[..Math.Min(p.Value.Length, 20)]}...")));

        using var content = new FormUrlEncodedContent(parameters);
        var response = await httpClient.PostAsync(new Uri(tokenEndpoint), content, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        this.logger.LogInformation(
            "Google token response - Status: {Status}, Content: {Content}",
            (int)response.StatusCode,
            responseContent); // Log full response to see if id_token is present

        response.EnsureSuccessStatusCode();

        // Manual JSON parsing to extract id_token since JsonPropertyName doesn't work
        this.logger.LogInformation("Raw JSON response: {Json}", responseContent);

        // Parse JSON manually to get all fields
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;

        var tokenResponse = new TokenResponseDto
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? string.Empty,
            RefreshToken = root.GetProperty("refresh_token").GetString() ?? string.Empty,
            IdToken = root.GetProperty("id_token").GetString() ?? string.Empty,
            TokenType = root.GetProperty("token_type").GetString() ?? string.Empty,
            ExpiresIn = root.GetProperty("expires_in").GetInt32(),
            Scope = root.GetProperty("scope").GetString() ?? string.Empty
        };

        this.logger.LogInformation(
            "TokenResponse fields - AccessToken: {AccessToken}, RefreshToken: {RefreshToken}, IdToken: {IdToken}, TokenType: {TokenType}, ExpiresIn: {ExpiresIn}, Scope: {Scope}",
            tokenResponse.AccessToken[..Math.Min(tokenResponse.AccessToken.Length, 20)] + "...",
            string.IsNullOrEmpty(tokenResponse.RefreshToken) ? "NULL" : tokenResponse.RefreshToken[..Math.Min(tokenResponse.RefreshToken.Length, 20)] + "...",
            string.IsNullOrEmpty(tokenResponse.IdToken) ? "NULL" : tokenResponse.IdToken[..Math.Min(tokenResponse.IdToken.Length, 20)] + "...",
            tokenResponse.TokenType,
            tokenResponse.ExpiresIn,
            tokenResponse.Scope);

        return tokenResponse;
    }

    private async Task<TokenResponseDto> ExchangeGitHubCodeAsync(HttpClient httpClient, string code, string codeVerifier, CancellationToken cancellationToken)
    {
        var settings = this.oauthSettings.GitHub;
        var tokenEndpoint = "https://github.com/login/oauth/access_token";

        // GitHub doesn't support PKCE, but we log the verifier for consistency
        this.logger.LogDebug("GitHub OAuth called with code verifier (not supported by GitHub): {CodeVerifier}", codeVerifier[..Math.Min(codeVerifier.Length, 20)]);

        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = settings.RedirectUri.ToString()
        };

        using var content = new FormUrlEncodedContent(parameters);

        // GitHub requires Accept header to return JSON instead of form-encoded data
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var response = await httpClient.PostAsync(new Uri(tokenEndpoint), content, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        this.logger.LogInformation(
            "GitHub token response - Status: {Status}, Content: {Content}",
            (int)response.StatusCode,
            responseContent);

        response.EnsureSuccessStatusCode();

        // Manual JSON parsing like Google
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;

        var tokenResponse = new TokenResponseDto
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? string.Empty,
            RefreshToken = root.TryGetProperty("refresh_token", out var refreshToken) ? refreshToken.GetString() ?? string.Empty : string.Empty,
            TokenType = root.GetProperty("token_type").GetString() ?? string.Empty,
            Scope = root.TryGetProperty("scope", out var scope) ? scope.GetString() ?? string.Empty : string.Empty
        };

        this.logger.LogInformation(
            "GitHub TokenResponse fields - AccessToken: {AccessToken}, TokenType: {TokenType}, Scope: {Scope}",
            tokenResponse.AccessToken[..Math.Min(tokenResponse.AccessToken.Length, 20)] + "...",
            tokenResponse.TokenType,
            tokenResponse.Scope);

        return tokenResponse;
    }
}

// Helper DTOs
internal sealed class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}
