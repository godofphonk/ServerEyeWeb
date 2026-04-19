#pragma warning disable SA1202 // 'public' members should come before 'private' members

namespace ServerEye.Core.Services;

using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Services;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public TimeSpan AccessTokenExpiration { get; set; }
    public TimeSpan RefreshTokenExpiration { get; set; }
    public string PrivateKeyBase64 { get; set; } = string.Empty;
    public string PublicKeyBase64 { get; set; } = string.Empty;
}

public sealed class JwtService : IJwtService
{
    private static ILogger<JwtService>? staticLogger;
    private readonly JwtSettings jwtSettings;
    private readonly RSA rsaPublicKey;
    private readonly RSA rsaPrivateKey;
    private readonly ILogger<JwtService>? logger;

    public JwtService(JwtSettings jwtSettings, IConfiguration configuration, ILogger<JwtService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(jwtSettings);
        ArgumentNullException.ThrowIfNull(configuration);

        this.jwtSettings = jwtSettings;
        this.logger = logger;
        staticLogger = logger;

        // Load RSA keys from JwtSettings
        if (!string.IsNullOrEmpty(jwtSettings.PrivateKeyBase64) && !string.IsNullOrEmpty(jwtSettings.PublicKeyBase64))
        {
            this.rsaPrivateKey = LoadRsaKeyFromBase64(jwtSettings.PrivateKeyBase64);
            this.rsaPublicKey = LoadRsaKeyFromBase64(jwtSettings.PublicKeyBase64);
            logger?.LogInformation("Loaded RSA keys from JwtSettings");
        }
        else
        {
            throw new InvalidOperationException(
                "JWT PrivateKeyBase64 and PublicKeyBase64 must be configured. " +
                "Please add them to appsettings.Development.json or set via environment variables.");
        }
    }

    private static RSA LoadRsaKeyFromBase64(string base64Key)
    {
        try
        {
            staticLogger?.LogInformation("Original key length: {Length}", base64Key.Length);

            // Remove whitespace and newlines
            var cleanedKey = base64Key.Replace("\n", string.Empty, StringComparison.Ordinal)
                                      .Replace("\r", string.Empty, StringComparison.Ordinal)
                                      .Replace(" ", string.Empty, StringComparison.Ordinal)
                                      .Replace("\t", string.Empty, StringComparison.Ordinal);

            staticLogger?.LogInformation("Cleaned key length: {Length}", cleanedKey.Length);

            // Log first 100 characters for debugging
            var keyPreview = cleanedKey.Length > 100 ? cleanedKey[..100] : cleanedKey;
            staticLogger?.LogInformation("Key preview (first 100 chars): {Preview}", keyPreview);

            // Check if key has PEM headers
            if (base64Key.Contains("-----BEGIN", StringComparison.Ordinal))
            {
                staticLogger?.LogInformation("Key has PEM headers, importing directly");
                var rsa = RSA.Create();
                rsa.ImportFromPem(base64Key);
                staticLogger?.LogInformation("Successfully loaded RSA key from PEM");
                return rsa;
            }

            // Key is in pure Base64 format, add PEM headers
            staticLogger?.LogInformation("Key is pure Base64, adding PEM headers");
            var pemKey = cleanedKey.Contains("MIICdwIBADANBgkqhkiG9w0BAQEFAASCAmEwggJd", StringComparison.Ordinal)
                ? $"-----BEGIN PRIVATE KEY-----\n{cleanedKey}\n-----END PRIVATE KEY-----"
                : $"-----BEGIN PUBLIC KEY-----\n{cleanedKey}\n-----END PUBLIC KEY-----";

            var rsaPem = RSA.Create();
            rsaPem.ImportFromPem(pemKey);
            staticLogger?.LogInformation("Successfully loaded RSA key from PEM with added headers");
            return rsaPem;
        }
        catch (Exception ex)
        {
            staticLogger?.LogError(ex, "Failed to load RSA key");
            throw new InvalidOperationException("Failed to load RSA key", ex);
        }
    }

    public string GenerateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new RsaSecurityKey(this.rsaPrivateKey)
        {
            KeyId = Guid.NewGuid().ToString()
        };

        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var expires = now.Add(this.jwtSettings.AccessTokenExpiration);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName),
            new Claim("role", user.Role.ToString().ToUpperInvariant()),
            new Claim("email_verified", user.IsEmailVerified.ToString().ToUpperInvariant()),
            new Claim("has_email", (!string.IsNullOrEmpty(user.Email)).ToString().ToUpperInvariant()),
            new Claim("has_password", user.HasPassword.ToString().ToUpperInvariant()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.Add(this.jwtSettings.AccessTokenExpiration).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            SigningCredentials = credentials,
            Issuer = this.jwtSettings.Issuer,
            Audience = this.jwtSettings.Audience
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Log token generation details
        this.logger?.LogInformation(
            "Generated access token - Now: {Now}, Expires: {Expires}, Expiration: {Expiration}",
            now.ToString("yyyy-MM-dd HH:mm:ss"),
            expires.ToString("yyyy-MM-dd HH:mm:ss"),
            this.jwtSettings.AccessTokenExpiration);

        return tokenString;
    }

    public string GenerateRefreshToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new RsaSecurityKey(this.rsaPrivateKey)
        {
            KeyId = Guid.NewGuid().ToString()
        };

        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim("type", "refresh"),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.Add(this.jwtSettings.RefreshTokenExpiration).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(this.jwtSettings.RefreshTokenExpiration),
            SigningCredentials = credentials,
            Issuer = this.jwtSettings.Issuer,
            Audience = this.jwtSettings.Audience
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        return ValidateToken(token, validateLifetime: true);
    }

    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new RsaSecurityKey(this.rsaPublicKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = this.jwtSettings.Issuer,
            ValidAudience = this.jwtSettings.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch (SecurityTokenValidationException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public bool IsTokenExpired(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return true;
        }

        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public string GetUserIdFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return string.Empty;
        }

        var principal = ValidateToken(token);
        return principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
    }
}
