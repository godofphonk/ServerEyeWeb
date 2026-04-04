namespace ServerEye.Core.Services.OAuth;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services;

public abstract class BaseOAuthProvider
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = null
    };

    protected BaseOAuthProvider(
        OAuthSettings oauthSettings,
        ILogger logger)
    {
        Settings = oauthSettings;
        Logger = logger;
    }

    public OAuthSettings Settings { get; }
    public ILogger Logger { get; }

    public abstract OAuthProvider ProviderType { get; }
    public abstract Task<OAuthChallengeResponseDto> CreateChallengeAsync(string state, string codeChallenge, Uri? returnUrl);
    public abstract Task<OAuthUserInfoDto> GetUserInfoAsync(string accessToken, string? idToken, CancellationToken cancellationToken);
    public abstract Task<TokenResponseDto> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken);
    public abstract bool IsEnabled();

    public virtual async Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            await GetUserInfoAsync(accessToken, null, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Protected utility methods
    protected static string GenerateSecureRandomString(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("=", string.Empty, StringComparison.Ordinal)[..length];
    }

    protected static byte[] SHA256Hash(string input)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(input));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:Do not use Uri as return type", Justification = "These are utility methods for base64 encoding/decoding, not URI creation")]
    protected static string EncodeBase64Url(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("=", string.Empty, StringComparison.Ordinal);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:Do not use Uri as return type", Justification = "These are utility methods for base64 encoding/decoding, not URI creation")]
    protected static string DecodeBase64Url(string input)
    {
        var padded = input.Length % 4 == 0 ? input : input.PadRight(input.Length + (4 - (input.Length % 4)), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
    }

    protected static async Task<T> ParseJsonResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<T>(content, JsonOptions)
               ?? throw new InvalidOperationException("Failed to parse response");
    }

    protected static async Task<Dictionary<string, object>> ParseJsonDictionaryAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<Dictionary<string, object>>(content, JsonOptions)
               ?? throw new InvalidOperationException("Failed to parse response");
    }
}
