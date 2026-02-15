namespace ServerEye.Core.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ServerEye.Core.Interfaces.Services;

public class WebSocketTokenService : IWebSocketTokenService
{
    private readonly JwtSettings jwtSettings;

    public WebSocketTokenService(JwtSettings jwtSettings) => this.jwtSettings = jwtSettings;

    public string GenerateToken(Guid userId, string serverId, TimeSpan ttl)
    {
        var claims = new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim("server_id", serverId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: this.jwtSettings.Issuer,
            audience: this.jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(ttl),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
