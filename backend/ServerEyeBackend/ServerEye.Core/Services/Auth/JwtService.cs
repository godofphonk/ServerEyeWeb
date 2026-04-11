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

        // Check if JWT_DEV_PRIVATE_KEY is set
        var devPrivateKey = Environment.GetEnvironmentVariable("JWT_DEV_PRIVATE_KEY");
        logger?.LogInformation("JWT_DEV_PRIVATE_KEY is {Status}", string.IsNullOrEmpty(devPrivateKey) ? "NOT SET" : "SET");

        // Load RSA keys from Doppler or use static key for development
        if (!string.IsNullOrEmpty(jwtSettings.PrivateKeyBase64) && !string.IsNullOrEmpty(jwtSettings.PublicKeyBase64))
        {
            // Production: Load keys from Doppler
            this.rsaPrivateKey = LoadRsaKeyFromBase64(jwtSettings.PrivateKeyBase64);
            this.rsaPublicKey = LoadRsaKeyFromBase64(jwtSettings.PublicKeyBase64);
            logger?.LogInformation("Loaded RSA keys from JwtSettings (Production)");
        }
        else
        {
            // Development: Use static key
            this.rsaPrivateKey = StaticRsaKey;
            this.rsaPublicKey = StaticRsaKey;
            logger?.LogInformation("Loaded RSA static key (Development)");
        }
    }

    public static RSA GetStaticRsaKey => StaticRsaKey;

    private static RSA StaticRsaKey { get; } = CreateStaticRsaKey();

#pragma warning disable CA1303
    private static RSA CreateStaticRsaKey()
    {
        // For development, load RSA key from environment variable
        // This ensures JWT tokens remain valid between development sessions
        // and removes hardcoded secrets from source code
        var devPrivateKey = Environment.GetEnvironmentVariable("JWT_DEV_PRIVATE_KEY");

        if (string.IsNullOrEmpty(devPrivateKey))
        {
            // Fallback: generate dynamic key if environment variable not set
            Console.WriteLine("[JwtService] JWT_DEV_PRIVATE_KEY not set, generating dynamic RSA key");
            return RSA.Create(2048);
        }

        try
        {
            // Convert \n escaped string to actual newlines
            var formattedKey = devPrivateKey.Replace("\\n", "\n", StringComparison.Ordinal);
            var rsa = RSA.Create();
            rsa.ImportFromPem(formattedKey);
            Console.WriteLine("[JwtService] Successfully loaded RSA key from JWT_DEV_PRIVATE_KEY");
            return rsa;
        }
        catch (Exception ex)
        {
            // Fallback to dynamic key if key import fails
            Console.WriteLine($"[JwtService] Failed to load RSA key from JWT_DEV_PRIVATE_KEY: {ex.Message}, generating dynamic RSA key");
            return RSA.Create(2048);
        }
    }
#pragma warning restore CA1303

    private static RSA LoadRsaKeyFromBase64(string base64Key)
    {
        try
        {
            var keyBytes = Convert.FromBase64String(base64Key);
            var rsa = RSA.Create();

            // Try to import as private key first
            try
            {
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
                return rsa;
            }
            catch
            {
                // If that fails, try as public key
                try
                {
                    rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
                    return rsa;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Invalid RSA key format", ex);
                }
            }
        }
        catch (Exception ex)
        {
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
