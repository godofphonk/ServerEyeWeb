namespace ServerEye.API.Configuration.Extensions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            var rsaKey = JwtService.GetStaticRsaKey;

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
        });

        services.AddAuthorization();

        // Register OAuth settings
        var oauthSettings = new OAuthSettings();

        // .NET doesn't convert OAUTH_TELEGRAM_BOTID to OAuth__Telegram__Botid properly
        // Use environment variables directly
        oauthSettings.Telegram.BotId = configuration["OAUTH_TELEGRAM_BOTID"] ?? string.Empty;
        oauthSettings.Telegram.BotToken = configuration["OAUTH_TELEGRAM_BOTTOKEN"] ?? string.Empty;
        oauthSettings.Telegram.RedirectUri = new Uri(configuration["OAuth:Telegram:RedirectUri"] ?? "https://127.0.0.1");
        oauthSettings.Telegram.Enabled = bool.Parse(configuration["OAuth:Telegram:Enabled"] ?? "true");

        oauthSettings.Google.ClientId = configuration["OAuth:Google:ClientId"] ?? string.Empty;
        oauthSettings.Google.ClientSecret = configuration["OAuth:Google:ClientSecret"] ?? string.Empty;
        oauthSettings.Google.RedirectUri = new Uri(configuration["OAuth:Google:RedirectUri"] ?? "https://127.0.0.1");
        oauthSettings.Google.Enabled = bool.Parse(configuration["OAuth:Google:Enabled"] ?? "true");

        oauthSettings.GitHub.ClientId = configuration["OAuth:GitHub:ClientId"] ?? string.Empty;
        oauthSettings.GitHub.ClientSecret = configuration["OAuth:GitHub:ClientSecret"] ?? string.Empty;
        oauthSettings.GitHub.RedirectUri = new Uri(configuration["OAuth:GitHub:RedirectUri"] ?? "https://127.0.0.1");
        oauthSettings.GitHub.Enabled = bool.Parse(configuration["OAuth:GitHub:Enabled"] ?? "true");

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
}
