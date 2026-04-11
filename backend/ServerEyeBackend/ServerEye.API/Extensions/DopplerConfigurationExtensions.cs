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
            var prefixes = new[] { "DATABASE_", "REDIS_", "JWT_", "STRIPE_", "EMAIL_", "GO_API_" };
            
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

        // Convert environment variable style to configuration sections
        // OAuth settings are read directly from environment variables in AuthenticationSetup.cs
        var replacements = new Dictionary<string, string>
        {
            ["JWT_"] = "JwtSettings:",
            ["ENCRYPTION_"] = "Encryption:",
            ["DATABASE_"] = "ConnectionStrings:",
            ["REDIS_"] = "Redis:",
            ["EMAIL_"] = "EmailSettings:",
            ["GO_API_"] = "GoApiSettings:",
            ["CORS_"] = "Cors:",
            ["RATE_LIMITING_"] = "RateLimiting:",
            ["SECURITY_"] = "Security:",
            ["CACHE_"] = "CacheSettings:",
            ["STRIPE_"] = "Stripe:"
        };

        var key = envVar;
        foreach (var replacement in replacements)
        {
            if (key.StartsWith(replacement.Key))
            {
                key = replacement.Value + key.Substring(replacement.Key.Length);
                break;
            }
        }

        // Convert snake_case to PascalCase for configuration sections
        key = string.Join("", key.Split('_', ':').Select(part =>
            char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant()));

        return key;
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
