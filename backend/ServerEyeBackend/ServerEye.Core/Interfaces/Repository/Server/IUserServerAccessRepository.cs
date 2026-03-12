namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;
using ServerEye.Core.Enums;

public interface IUserServerAccessRepository
{
    public Task<List<Server>> GetUserServersAsync(Guid userId);
    public Task<List<User>> GetServerUsersAsync(string serverId);
    public Task<bool> HasAccessAsync(Guid userId, string serverId);
    public Task<AccessLevel?> GetAccessLevelAsync(Guid userId, string serverId);
    public Task AddAccessAsync(UserServerAccess access);
    public Task RemoveAccessAsync(Guid userId, string serverId);
    public Task UpdateAccessLevelAsync(Guid userId, string serverId, AccessLevel level);
}
