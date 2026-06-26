namespace ServerEye.API.Configuration.Extensions;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using ServerEye.Core.Configuration;
using ServerEye.Core.Services;

/// <summary>
/// Authentication and authorization configuration.
/// </summary>
public static class AuthenticationSetup
{
    /// <summary>
    /// Adds JWT authentication and OAuth configuration.
    /// </summary>
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? new JwtSettings();

        // Validate JWT settings
        ValidateJwtSettings(jwtSettings);

        // Configure JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Load RSA key from JwtSettings
            RSA rsaKey;
            if (!string.IsNullOrEmpty(jwtSettings.PrivateKey) && !string.IsNullOrEmpty(jwtSettings.PublicKey))
            {
                rsaKey = LoadRsaKeyFromBase64(jwtSettings.PublicKey);
            }
            else
            {
                throw new InvalidOperationException(
                    "JWT PrivateKey and PublicKey must be configured. " +
                    "Please add them to Doppler dev config.");
            }

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new RsaSecurityKey(rsaKey),
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true
            };

            // Add blacklist check using OnTokenValidated event
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
                    if (context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken token)
                    {
                        var tokenString = token.RawData;
                        var blacklisted = await cache.GetStringAsync($"blacklist:{tokenString}");
                        if (!string.IsNullOrEmpty(blacklisted))
                        {
                            context.Fail("Token has been revoked");
                        }
                    }
                }
            };
        });

        services.AddAuthorization();

        // Register OAuth settings
        var oauthSettings = new OAuthSettings();

        // .NET doesn't convert OAUTH_TELEGRAM_BOTID to OAuth__Telegram__Botid properly
        // Use environment variables directly for all OAuth settings
        oauthSettings.Telegram.BotId = configuration["OAUTH_TELEGRAM_BOT_ID"] ?? string.Empty;
        oauthSettings.Telegram.BotToken = configuration["OAUTH_TELEGRAM_BOT_TOKEN"] ?? string.Empty;
        oauthSettings.Telegram.RedirectUri = new Uri(configuration["OAUTH_TELEGRAM_REDIRECT_URI"] ?? "https://127.0.0.1");
        oauthSettings.Telegram.Enabled = bool.Parse(configuration["OAUTH_TELEGRAM_ENABLED"] ?? "true");

        oauthSettings.Google.ClientId = configuration["OAUTH_GOOGLE_CLIENT_ID"] ?? string.Empty;
        oauthSettings.Google.ClientSecret = configuration["OAUTH_GOOGLE_CLIENT_SECRET"] ?? string.Empty;
        oauthSettings.Google.RedirectUri = new Uri(configuration["OAUTH_GOOGLE_REDIRECT_URI"] ?? "https://127.0.0.1");
        oauthSettings.Google.Enabled = bool.Parse(configuration["OAUTH_GOOGLE_ENABLED"] ?? "true");

        oauthSettings.GitHub.ClientId = configuration["OAUTH_GITHUB_CLIENT_ID"] ?? string.Empty;
        oauthSettings.GitHub.ClientSecret = configuration["OAUTH_GITHUB_CLIENT_SECRET"] ?? string.Empty;
        oauthSettings.GitHub.RedirectUri = new Uri(configuration["OAUTH_GITHUB_REDIRECT_URI"] ?? "https://127.0.0.1");
        oauthSettings.GitHub.Enabled = bool.Parse(configuration["OAUTH_GITHUB_ENABLED"] ?? "true");

        services.AddSingleton(oauthSettings);

        // Register OAuth providers
        services.AddScoped<Core.Services.OAuth.Providers.GoogleOAuthProvider>();
        services.AddScoped<Core.Services.OAuth.Providers.GitHubOAuthProvider>();
        services.AddScoped<Core.Services.OAuth.Providers.TelegramOAuthProvider>();

        // Register OAuth provider factory
        services.AddScoped<Core.Services.OAuth.Factory.IOAuthProviderFactory,
            Core.Services.OAuth.Factory.OAuthProviderFactory>();

        return services;
    }

    /// <summary>
    /// Validates JWT configuration settings.
    /// </summary>
    private static void ValidateJwtSettings(JwtSettings jwtSettings)
    {
        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
        {
            throw new InvalidOperationException("JWT Issuer is required but not configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
        {
            throw new InvalidOperationException("JWT Audience is required but not configured.");
        }

        if (jwtSettings.AccessTokenExpiration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("JWT AccessTokenExpiration must be greater than 0.");
        }

        if (jwtSettings.RefreshTokenExpiration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("JWT RefreshTokenExpiration must be greater than 0.");
        }
    }

    /// <summary>
    /// Loads RSA key from Base64 string (same logic as JwtService for consistency).
    /// </summary>
    private static RSA LoadRsaKeyFromBase64(string base64Key)
    {
        try
        {
            // Check if key has PEM headers
            if (base64Key.Contains("-----BEGIN", StringComparison.Ordinal))
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(base64Key);
                return rsa;
            }

            // Remove whitespace and newlines
            var cleanedKey = base64Key.Replace("\n", string.Empty, StringComparison.Ordinal)
                                      .Replace("\r", string.Empty, StringComparison.Ordinal)
                                      .Replace(" ", string.Empty, StringComparison.Ordinal)
                                      .Replace("\t", string.Empty, StringComparison.Ordinal);

            // Key is in pure Base64 format, add PEM headers
            var pemKey = cleanedKey.Contains("MIICdwIBADANBgkqhkiG9w0BAQEFAASCAmEwggJd", StringComparison.Ordinal)
                ? $"-----BEGIN PRIVATE KEY-----\n{cleanedKey}\n-----END PRIVATE KEY-----"
                : $"-----BEGIN PUBLIC KEY-----\n{cleanedKey}\n-----END PUBLIC KEY-----";

            var rsaPem = RSA.Create();
            rsaPem.ImportFromPem(pemKey);
            return rsaPem;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load RSA key", ex);
        }
    }
}
