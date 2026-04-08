using System.Text.Json;
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
/// Extension methods for adding Doppler secrets to IConfiguration
/// </summary>
public static class DopplerConfigurationExtensions
{
    /// <summary>
    /// Adds Doppler secrets to the configuration
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="project">Doppler project name</param>
    /// <param name="config">Doppler config name (environment)</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>The configuration builder for chaining</returns>
    public static IConfigurationBuilder AddDopplerSecrets(this IConfigurationBuilder builder, string project, string config, ILogger? logger = null)
    {
        builder.Add(new DopplerConfigurationSource(project, config, logger));
        return builder;
    }

    /// <summary>
    /// Adds Doppler secrets from environment variables (injected by doppler run)
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
/// Doppler configuration provider
/// </summary>
public class DopplerConfigurationProvider : ConfigurationProvider
{
    private readonly string _project;
    private readonly string _config;
    private readonly ILogger? _logger;

    public DopplerConfigurationProvider(string project, string config, ILogger? logger = null)
    {
        _project = project;
        _config = config;
        _logger = logger;
    }

    public override void Load()
    {
        try
        {
            var secrets = GetDopplerSecretsAsync().GetAwaiter().GetResult();

            if (secrets != null)
            {
                foreach (var secret in secrets)
                {
                    Data[secret.Key] = secret.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading Doppler secrets");
            // Don't throw - allow fallback to other configuration sources
        }
    }

    private async Task<Dictionary<string, string>?> GetDopplerSecretsAsync()
    {
        try
        {
            var token = Environment.GetEnvironmentVariable("DOPPLER_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                _logger?.LogWarning("DOPPLER_TOKEN environment variable not found");
                return null;
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var url = $"https://api.doppler.com/v3/configs/config/secrets/list?project={_project}&config={_config}";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger?.LogError("Doppler API error: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var dopplerResponse = JsonSerializer.Deserialize<DopplerSecretsResponse>(json);

            if (dopplerResponse?.Secrets == null)
            {
                _logger?.LogWarning("No secrets found in Doppler response");
                return null;
            }

            // Convert flat secrets to hierarchical configuration
            var secrets = new Dictionary<string, string>();
            foreach (var secret in dopplerResponse.Secrets)
            {
                // Convert environment variable style to configuration sections
                var key = ConvertEnvironmentVariableToConfigurationKey(secret.Name);
                secrets[key] = secret.Value;
            }

            _logger?.LogInformation("Loaded {Count} secrets from Doppler", secrets.Count);
            return secrets;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching Doppler secrets");
            return null;
        }
    }

    private static string ConvertEnvironmentVariableToConfigurationKey(string envVar)
    {
        // Convert JWT_SECRET_KEY to JwtSettings:SecretKey
        // Convert DATABASE_CONNECTION_STRING to ConnectionStrings:Database

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
            ["STRIPE_"] = "Stripe:",
            ["OAUTH_"] = "OAuth:"
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
        // But keep OAuth:TelegramBotId as is (don't convert BotId to Botid)
        if (key.StartsWith("OAuth:Telegram"))
        {
            // Special handling for OAuth:Telegram* - keep BotId, BotToken as is
            return key;
        }

        key = string.Join("", key.Split('_', ':').Select(part =>
            char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant()));

        return key;
    }
}

/// <summary>
/// Doppler configuration source
/// </summary>
public class DopplerConfigurationSource : IConfigurationSource
{
    private readonly string _project;
    private readonly string _config;
    private readonly ILogger? _logger;

    public DopplerConfigurationSource(string project, string config, ILogger? logger = null)
    {
        _project = project;
        _config = config;
        _logger = logger;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DopplerConfigurationProvider(_project, _config, _logger);
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
            var prefixes = new[] { "DATABASE_", "REDIS_", "JWT_", "STRIPE_", "OAUTH_", "EMAIL_" };
            
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
            ["STRIPE_"] = "Stripe:",
            ["OAUTH_"] = "OAuth:"
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
        // But keep OAuth:TelegramBotId as is (don't convert BotId to Botid)
        if (key.StartsWith("OAuth:Telegram"))
        {
            // Special handling for OAuth:Telegram* - keep BotId, BotToken as is
            return key;
        }

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

/// <summary>
/// Doppler API response models
/// </summary>
public class DopplerSecretsResponse
{
    public List<DopplerSecret> Secrets { get; set; } = new();
}

public class DopplerSecret
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
