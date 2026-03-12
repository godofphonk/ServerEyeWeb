namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.Entities;
using System.Security.Claims;

public interface IJwtService
{
    public string GenerateAccessToken(User user);
    public string GenerateRefreshToken(User user);
    public ClaimsPrincipal? ValidateToken(string token);
    public bool IsTokenExpired(string token);
    public string GetUserIdFromToken(string token);
}
