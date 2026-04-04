namespace ServerEye.Core.Interfaces.Services;

using System.Security.Claims;
using ServerEye.Core.Entities;

public interface IJwtService
{
    public string GenerateAccessToken(User user);
    public string GenerateRefreshToken(User user);
    public ClaimsPrincipal? ValidateToken(string token);
    public bool IsTokenExpired(string token);
    public string GetUserIdFromToken(string token);
}
