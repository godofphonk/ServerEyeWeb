#pragma warning disable SA1202 // 'public' members should come before 'private' members

namespace ServerEye.Core.Services;

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
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
    private readonly JwtSettings jwtSettings;
    private readonly RSA rsaPublicKey;
    private readonly RSA rsaPrivateKey;

    public JwtService(JwtSettings jwtSettings, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(jwtSettings);
        ArgumentNullException.ThrowIfNull(configuration);

        this.jwtSettings = jwtSettings;

        // Load RSA keys from Doppler or use static key for development
        if (!string.IsNullOrEmpty(jwtSettings.PrivateKeyBase64) && !string.IsNullOrEmpty(jwtSettings.PublicKeyBase64))
        {
            // Production: Load keys from Doppler
            this.rsaPrivateKey = LoadRsaKeyFromBase64(jwtSettings.PrivateKeyBase64);
            this.rsaPublicKey = LoadRsaKeyFromBase64(jwtSettings.PublicKeyBase64);
        }
        else
        {
            // Development: Use static key
            this.rsaPrivateKey = StaticRsaKey;
            this.rsaPublicKey = StaticRsaKey;
        }
    }

    public static RSA GetStaticRsaKey => StaticRsaKey;

    private static RSA StaticRsaKey { get; } = CreateStaticRsaKey();

    private static RSA CreateStaticRsaKey()
    {
        // For development, create a static RSA key that persists across app restarts
        // This ensures JWT tokens remain valid between development sessions
        const string staticPrivateKey = """
            -----BEGIN PRIVATE KEY-----
            MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCdGPpDWsumv0Yi
            BaNgU+TC5TkG3skbuNpncm0PjJO9Usvw+7HOxXzyKsYz9lu4e6dC2eYzUzYvM1gH
            QxSdv/KRBVyiswp491WHqK6k0P0XlbqwEYkAfFoVu4S0rVh/psu7tW5rqtaVCkVz
            tI6K5paDW3oQv9B2JUGiC19IKHXFfu+CLVG/YCdyiu0zd2HUf81tIigg2aHGLSfQ
            feTjcFO74xUUl+svHuFC3cWFBASyJMXlG+qZDByx3QAdArnuukEO4TJ/yaF5sl1F
            5NmquNqRtdq7n4meRPWkqnuoyFFWyVvORgwgLJyw+sGV+T3DNHez7zxwUG68cZEe
            ImQz1EaBAgMBAAECggEAA/Z7xigPvfIiYZRzNIhrO24bNhjXuX38TuqaVQ+EkMya
            Ucb0m1jcm/J/p1MIKWc/nvJmkd4ADnjg6CZ9WjUbyCQarA1AhvCEySA0fxp5Pu9/
            SwaXNlKctmKBgIoEouw1BJQ5H8jKgs4UdkysQZVbX78GjU58YoWSMjVvmu8v2Npu
            rh15enWG5v7B5A81QXCk5cP/LD4EGdzIyKnMKtUOg8lPLI10ayW1maVpTkKL6Rol
            4ym4vB+QFyXOuixHaVRYfhUgJsx/26fpOGNgQgfGM5GjUaLc21a9RWROrw8D8pQP
            pXspMQurrdevjEtK+fdZag2Abc+/qs8q5UGUwF2RUQKBgQDP3LjpoKNt2pntAtFD
            c1qOUWqxmG68LK95Y5Z+ejzG6MLMc85g15nnzTRfUL6ZeyI5N4Jm79esEwoKKVzH
            uM2bdeMuM2DZU2yfRwssNs4mlAo0A3j52hKg0hvu6QCfsB60neNILqC0PHAB4iM0
            v7auwoTGa0jlJSWpSCeOz5hvcQKBgQDBeqIpKdtR8gpu/Ho1PGmhGuaToxwrRd56
            6oPqDdett2feztEJLd+VNJVODzT3535u3A7PuidexSV/QDFWwsLzjrv+e94WVefr
            ujdoieQk35//IuSpzvab6wpXsbmyvWZHrWGqFIuNyYJWBNRCyqEjiYNlJABegQh0
            fgl3R1LgEQKBgDPZv1an95yDlzoEJedJcyFlNdQvThAqpWsGaJgMLfUAQvd1O9n0
            bjPggFv2bFUk3hifvCupUIdgCHUYdEht1PweoBj6QAJ2SPZCZosU8L+21gS7iQXq
            XBM51jX2cW1kJYSwje2HlBbhrJ8LpfSWjh9x7mUAhiKC7a4YjaWWK1RBAoGAJyn9
            vTtdy96kwgaVbkVGVHgviF8SCqhf+p2SCkS3DdD8U5ulsKf6hCdauaxWWoAfla0x
            ylayNXrOtk12L0vJTqfr4f2M3RSSl6LgKGcRKW2i43BavQzJ2pHfTBULs+Sm2Yd9
            J4J1JURO/76GgOana5wgXs7EzFxuK7Z/kAd9/SECgYBu3gsVtLkySmH2RYbGWcy+
            s8SOvaYciYVZUMRMn6mZHk3EIHLP4Bm8wCCq57lunnyuxBR2wnhb/B1KMHAHXDcK
            fiOJk1e5iWfcs1pAJKl6QPsfet7zefm1K3YpYoUexnSVTtiq5IW+Vg58s1W7WsVN
            10uo6TwyxXGiUOGV3EJYUg==
            -----END PRIVATE KEY-----
            """;

        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(staticPrivateKey);
            return rsa;
        }
        catch
        {
            // Fallback to dynamic key if static key fails
            return RSA.Create(2048);
        }
    }

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

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName),
            new Claim("role", user.Role.ToString().ToUpperInvariant()),
            new Claim("email_verified", user.IsEmailVerified.ToString().ToUpperInvariant()),
            new Claim("has_email", (!string.IsNullOrEmpty(user.Email)).ToString().ToUpperInvariant()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.Add(this.jwtSettings.AccessTokenExpiration).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(this.jwtSettings.AccessTokenExpiration),
            SigningCredentials = credentials,
            Issuer = this.jwtSettings.Issuer,
            Audience = this.jwtSettings.Audience
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
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
            ValidateLifetime = true,
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
