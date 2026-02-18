namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Enums;

public interface IServerAccessService
{
    public Task<bool> HasAccessAsync(Guid userId, string serverId);
    public Task<AccessLevel?> GetAccessLevelAsync(Guid userId, string serverId);
    public Task<List<ServerResponse>> GetUserServersAsync(Guid userId);
    public Task<ServerResponse> AddServerAsync(Guid userId, string serverKey);
    public Task RemoveServerAsync(Guid userId, string serverId);
    public Task ShareServerAsync(Guid ownerId, string serverId, string targetUserEmail, AccessLevel level);
}
