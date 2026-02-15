namespace ServerEye.Core.Interfaces.Services;

public interface IWebSocketTokenService
{
    public string GenerateToken(Guid userId, string serverId, TimeSpan ttl);
}
