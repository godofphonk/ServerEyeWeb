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
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public TimeSpan AccessTokenExpiration { get; set; }
    public TimeSpan RefreshTokenExpiration { get; set; }
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
}

public sealed class JwtService : IJwtService, IDisposable
{
    private readonly JwtSettings jwtSettings;
    private readonly RSA rsaPublicKey;
    private readonly RSA rsaPrivateKey;
    private readonly ILogger<JwtService>? logger;
    private bool disposed;

    public JwtService(JwtSettings jwtSettings, ILogger<JwtService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(jwtSettings);

        this.jwtSettings = jwtSettings;
        this.logger = logger;

        // Load RSA keys from JwtSettings
        if (!string.IsNullOrEmpty(jwtSettings.PrivateKey) && !string.IsNullOrEmpty(jwtSettings.PublicKey))
        {
            this.rsaPrivateKey = LoadPrivateKey(jwtSettings.PrivateKey);
            this.rsaPublicKey = LoadPublicKey(jwtSettings.PublicKey);
            logger?.LogInformation("Loaded RSA keys from JwtSettings");
        }
        else
        {
            throw new InvalidOperationException(
                "JWT PrivateKey and PublicKey must be configured. " +
                "Please add them to appsettings.Development.json or set via environment variables.");
        }
    }

    private static string RemoveWhitespace(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c != '\n' && c != '\r' && c != ' ' && c != '\t')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private RSA LoadPrivateKey(string value)
    {
        try
        {
            var rsa = RSA.Create();

            if (value.Contains("-----BEGIN", StringComparison.Ordinal))
            {
                rsa.ImportFromPem(value);
                return rsa;
            }

            var bytes = Convert.FromBase64String(RemoveWhitespace(value));
            rsa.ImportPkcs8PrivateKey(bytes, out _);
            return rsa;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to load private RSA key");
            throw new InvalidOperationException("Failed to load private RSA key", ex);
        }
    }

    private RSA LoadPublicKey(string value)
    {
        try
        {
            var rsa = RSA.Create();

            if (value.Contains("-----BEGIN", StringComparison.Ordinal))
            {
                rsa.ImportFromPem(value);
                return rsa;
            }

            var bytes = Convert.FromBase64String(RemoveWhitespace(value));
            rsa.ImportSubjectPublicKeyInfo(bytes, out _);
            return rsa;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to load public RSA key");
            throw new InvalidOperationException("Failed to load public RSA key", ex);
        }
    }

    public string GenerateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new RsaSecurityKey(this.rsaPrivateKey)
        {
            KeyId = this.jwtSettings.KeyId
        };

        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var expires = now.Add(this.jwtSettings.AccessTokenExpiration);
        var nowOffset = new DateTimeOffset(now);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("type", "access"),
            new Claim(System.Security.Claims.ClaimTypes.Role, user.Role.ToString().ToUpperInvariant()),
            new Claim("email_verified", user.IsEmailVerified.ToString().ToUpperInvariant()),
            new Claim("has_email", (!string.IsNullOrEmpty(user.Email)).ToString().ToUpperInvariant()),
            new Claim("has_password", user.HasPassword.ToString().ToUpperInvariant()),
            new Claim(JwtRegisteredClaimNames.Iat, nowOffset.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
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
            KeyId = this.jwtSettings.KeyId
        };

        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var expires = now.Add(this.jwtSettings.RefreshTokenExpiration);
        var nowOffset = new DateTimeOffset(now);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("type", "refresh"),
            new Claim(JwtRegisteredClaimNames.Iat, nowOffset.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
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
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        return ValidateTokenInternal(token, validateLifetime: true);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        var principal = ValidateTokenInternal(token, validateLifetime: true);

        if (principal is null)
        {
            return null;
        }

        if (principal.FindFirst("type")?.Value != "access")
        {
            return null;
        }

        return principal;
    }

    public ClaimsPrincipal? ValidateRefreshToken(string token)
    {
        var principal = ValidateTokenInternal(token, validateLifetime: true);

        if (principal is null)
        {
            return null;
        }

        if (principal.FindFirst("type")?.Value != "refresh")
        {
            return null;
        }

        return principal;
    }

    public ClaimsPrincipal? ValidateExpiredAccessToken(string token)
    {
        var principal = ValidateTokenInternal(token, validateLifetime: false);

        if (principal?.FindFirst("type")?.Value != "access")
        {
            return null;
        }

        return principal;
    }

    private ClaimsPrincipal? ValidateTokenInternal(string token, bool validateLifetime)
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
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                jwtToken.Header.Alg != SecurityAlgorithms.RsaSha256)
            {
                return null;
            }

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

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.rsaPrivateKey?.Dispose();
        this.rsaPublicKey?.Dispose();
        this.disposed = true;
        GC.SuppressFinalize(this);
    }
}
