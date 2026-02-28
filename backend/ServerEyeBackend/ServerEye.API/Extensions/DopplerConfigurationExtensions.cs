using Microsoft.Extensions.Configuration;
using System.Text.Json;

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
    /// <returns>The configuration builder for chaining</returns>
    public static IConfigurationBuilder AddDopplerSecrets(this IConfigurationBuilder builder, string project, string config)
    {
        return builder.Add(new DopplerConfigurationSource(project, config));
    }
}

/// <summary>
/// Doppler configuration provider
/// </summary>
public class DopplerConfigurationProvider : ConfigurationProvider
{
    private readonly string _project;
    private readonly string _config;

    public DopplerConfigurationProvider(string project, string config)
    {
        _project = project;
        _config = config;
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
            Console.WriteLine($"Error loading Doppler secrets: {ex.Message}");
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
                Console.WriteLine("DOPPLER_TOKEN environment variable not found");
                return null;
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var url = $"https://api.doppler.com/v3/configs/config/secrets/list?project={_project}&config={_config}";
            
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Doppler API error: {response.StatusCode} - {error}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var dopplerResponse = JsonSerializer.Deserialize<DopplerSecretsResponse>(json);
            
            if (dopplerResponse?.Secrets == null)
            {
                Console.WriteLine("No secrets found in Doppler response");
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

            Console.WriteLine($"Loaded {secrets.Count} secrets from Doppler");
            return secrets;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Doppler secrets: {ex.Message}");
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
            ["CACHE_"] = "CacheSettings:"
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
/// Doppler configuration source
/// </summary>
public class DopplerConfigurationSource : IConfigurationSource
{
    private readonly string _project;
    private readonly string _config;

    public DopplerConfigurationSource(string project, string config)
    {
        _project = project;
        _config = config;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DopplerConfigurationProvider(_project, _config);
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
