namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;

public interface IServerRepository : IBaseRepository<ServerEntity>
{
    public Task<List<User>> GetServerUsersAsync(Guid serverId);
    public Task UpdateServerNameAsync(Guid serverId, string? serverName);
}
