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
        var oauthSettings = configuration.GetSection("OAuth").Get<OAuthSettings>() 
            ?? new OAuthSettings();
        services.AddSingleton(oauthSettings);

        // Register OAuth providers
        services.AddScoped<ServerEye.Core.Services.OAuth.Providers.GoogleOAuthProvider>();
        services.AddScoped<ServerEye.Core.Services.OAuth.Providers.GitHubOAuthProvider>();
        services.AddScoped<ServerEye.Core.Services.OAuth.Providers.TelegramOAuthProvider>();

        // Register OAuth provider factory
        services.AddScoped<ServerEye.Core.Services.OAuth.Factory.IOAuthProviderFactory, 
            ServerEye.Core.Services.OAuth.Factory.OAuthProviderFactory>();

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
