using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#pragma warning disable SA1629 // Documentation text should end with a period
#pragma warning disable SA1512 // Single-line comments should not be followed by blank line
#pragma warning disable SA1515 // Single-line comment should be preceded by blank line
#pragma warning disable CA2234 // Use string.Empty for empty strings
#pragma warning disable CA1303 // Localize strings
#pragma warning disable SA1122 // Use string.Empty for empty strings
#pragma warning disable CA1845 // Use string.Concat and AsSpan
#pragma warning disable CA1308 // Use ToUpperInvariant
#pragma warning disable CA1310 // Use StringComparison
#pragma warning disable IDE0057 // Simplify Substring
#pragma warning disable SA1204 // Static members should appear before non-static members

namespace ServerEye.API.Extensions;

/// <summary>
/// Extension methods for adding Doppler secrets from environment variables (Option 2 - doppler run)
/// </summary>
public static class DopplerConfigurationExtensions
{
    /// <summary>
    /// Adds Doppler secrets from environment variables (injected by doppler run from host)
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>The configuration builder for chaining</returns>
    public static IConfigurationBuilder AddDopplerSecretsFromEnvironment(this IConfigurationBuilder builder, ILogger? logger = null)
    {
        builder.Add(new DopplerEnvironmentConfigurationSource(logger));
        return builder;
    }
}

/// <summary>
/// Doppler environment configuration provider (reads from environment variables injected by doppler run)
/// </summary>
public class DopplerEnvironmentConfigurationProvider : ConfigurationProvider
{
    private readonly ILogger? _logger;

    public DopplerEnvironmentConfigurationProvider(ILogger? logger = null)
    {
        _logger = logger;
    }

    public override void Load()
    {
        try
        {
            var secrets = new Dictionary<string, string>();

            // Read all environment variables that start with Doppler secret prefixes
            // OAuth settings are read directly from environment variables in AuthenticationSetup.cs
            var prefixes = new[]
            {
                "DATABASE_", "REDIS_", "JWT_", "STRIPE_", "EMAIL_",
                "GO_API_", "GOAPI_SETTINGS_",
                "BILLING_DB_", "TICKET_DB_",
                "OAUTH_", "ENCRYPTION_"
            };

            foreach (var envVar in Environment.GetEnvironmentVariables().Keys)
            {
                if (envVar is string key)
                {
                    bool startsWithPrefix = false;
                    foreach (var prefix in prefixes)
                    {
                        if (key.StartsWith(prefix))
                        {
                            startsWithPrefix = true;
                            break;
                        }
                    }

                    if (startsWithPrefix || key.StartsWith("DOPPLER_"))
                    {
                        var value = Environment.GetEnvironmentVariable(key);
                        if (!string.IsNullOrEmpty(value))
                        {
                            var configKey = ConvertEnvironmentVariableToConfigurationKey(key);
                            secrets[configKey] = value;
                        }
                    }
                }
            }

            foreach (var secret in secrets)
            {
                Data[secret.Key] = secret.Value;
            }

            _logger?.LogInformation("Loaded {Count} secrets from environment variables", secrets.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading Doppler secrets from environment variables");
            // Don't throw - allow fallback to other configuration sources
        }
    }

    private static string ConvertEnvironmentVariableToConfigurationKey(string envVar)
    {
        // Skip DOPPLER_ variables as they are configuration for Doppler itself
        if (envVar.StartsWith("DOPPLER_"))
        {
            return envVar;
        }

        // Exact matches for compound names that shouldn't be processed by the general logic
        var exactMatches = new Dictionary<string, string>
        {
            ["BILLING_DB_CONNECTION_STRING"] = "ConnectionStrings:BillingDbContext",
            ["TICKET_DB_CONNECTION_STRING"] = "ConnectionStrings:TicketDbContext",
            ["DATABASE_CONNECTION_STRING"] = "ConnectionStrings:ServerEyeDbContext",
            ["REDIS_CONNECTION_STRING"] = "ConnectionStrings:Redis",
            ["EMAIL_SETTINGS_FRONTEND_URL"] = "EmailSettings:FrontendUrl",
            ["EMAIL_SETTINGS_AWS_ACCESS_KEY"] = "EmailSettings:AwsAccessKey",
            ["EMAIL_SETTINGS_AWS_SECRET_KEY"] = "EmailSettings:AwsSecretKey",
            ["EMAIL_SETTINGS_AWS_REGION"] = "EmailSettings:AwsRegion",
            ["EMAIL_SETTINGS_FROM_EMAIL"] = "EmailSettings:FromEmail",
            ["EMAIL_SETTINGS_FROM_NAME"] = "EmailSettings:FromName",
            ["EMAIL_SETTINGS_SUPPORT_EMAIL"] = "EmailSettings:SupportEmail",
            ["EMAIL_SETTINGS_USE_AWS_SES"] = "EmailSettings:UseAwsSes",
            ["EMAIL_SETTINGS_ENABLE_SSL"] = "EmailSettings:EnableSsl",
            ["GOAPI_SETTINGS_BASE_URL"] = "GoApiSettings:BaseUrl",
            ["OAUTH_GOOGLE_CLIENT_ID"] = "OAuth:Google:ClientId",
            ["OAUTH_GOOGLE_CLIENT_SECRET"] = "OAuth:Google:ClientSecret",
            ["OAUTH_GOOGLE_ENABLED"] = "OAuth:Google:Enabled",
            ["OAUTH_GOOGLE_REDIRECT_URI"] = "OAuth:Google:RedirectUri",
            ["OAUTH_GITHUB_CLIENT_ID"] = "OAuth:GitHub:ClientId",
            ["OAUTH_GITHUB_CLIENT_SECRET"] = "OAuth:GitHub:ClientSecret",
            ["OAUTH_GITHUB_ENABLED"] = "OAuth:GitHub:Enabled",
            ["OAUTH_GITHUB_REDIRECT_URI"] = "OAuth:GitHub:RedirectUri",
            ["OAUTH_TELEGRAM_BOT_ID"] = "OAuth:Telegram:BotId",
            ["OAUTH_TELEGRAM_BOT_TOKEN"] = "OAuth:Telegram:BotToken",
            ["OAUTH_TELEGRAM_ENABLED"] = "OAuth:Telegram:Enabled",
            ["OAUTH_TELEGRAM_REDIRECT_URI"] = "OAuth:Telegram:RedirectUri",
            ["JWT_PRIVATE_KEY_BASE64"] = "JwtSettings:PrivateKeyBase64",
            ["JWT_PUBLIC_KEY_BASE64"] = "JwtSettings:PublicKeyBase64",
            ["JWT_SECRET_KEY"] = "JwtSettings:SecretKey",
            ["ENCRYPTION_KEY"] = "Encryption:Key",
            ["STRIPE_SECRET_KEY"] = "Stripe:SecretKey",
            ["STRIPE_PUBLISHABLE_KEY"] = "Stripe:PublishableKey",
            ["STRIPE_WEBHOOK_SECRET"] = "Stripe:WebhookSecret"
        };

        if (exactMatches.TryGetValue(envVar, out var exactMatch))
        {
            return exactMatch;
        }

        // Convert environment variable style to configuration sections
        // OAuth settings are read directly from environment variables in AuthenticationSetup.cs
        var replacements = new Dictionary<string, string>
        {
            ["JWT_"] = "JwtSettings:",
            ["ENCRYPTION_"] = "Encryption:",
            ["DATABASE_"] = "ConnectionStrings:",
            ["REDIS_"] = "Redis:",
            ["BILLING_DB_"] = "ConnectionStrings:BillingDbContext:",
            ["TICKET_DB_"] = "ConnectionStrings:TicketDbContext:",
            ["GO_API_"] = "GoApiSettings:",
            ["GOAPI_SETTINGS_"] = "GoApiSettings:",
            ["EMAIL_SETTINGS_"] = "EmailSettings:",
            ["CORS_"] = "Cors:",
            ["RATE_LIMITING_"] = "RateLimiting:",
            ["SECURITY_"] = "Security:",
            ["CACHE_"] = "CacheSettings:",
            ["STRIPE_"] = "Stripe:",
            ["OAUTH_GOOGLE_"] = "OAuth:Google:",
            ["OAUTH_GITHUB_"] = "OAuth:GitHub:",
            ["OAUTH_TELEGRAM_"] = "OAuth:Telegram:"
        };

        var key = envVar;
        string section = "";
        foreach (var replacement in replacements)
        {
            if (key.StartsWith(replacement.Key))
            {
                section = replacement.Value;
                key = key.Substring(replacement.Key.Length);
                break;
            }
        }

        // Convert snake_case to camelCase for property names
        var parts = key.Split('_');
        var propertyName = string.Join("", parts.Select((part, index) =>
        {
            if (string.IsNullOrEmpty(part))
            {
                return "";
            }

            // First part: lowercase, subsequent parts: PascalCase
            if (index == 0)
            {
                return part.ToLowerInvariant();
            }

            return char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant();
        }));

        return section + propertyName;
    }
}

/// <summary>
/// Doppler environment configuration source
/// </summary>
public class DopplerEnvironmentConfigurationSource : IConfigurationSource
{
    private readonly ILogger? _logger;

    public DopplerEnvironmentConfigurationSource(ILogger? logger = null)
    {
        _logger = logger;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DopplerEnvironmentConfigurationProvider(_logger);
    }
}
