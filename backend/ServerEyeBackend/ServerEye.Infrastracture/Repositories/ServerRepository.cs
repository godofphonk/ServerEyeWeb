namespace ServerEye.Infrastracture.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;

public sealed class ServerRepository(ServerEyeDbContext context) : IServerRepository
{
    private readonly ServerEyeDbContext context = context;

    public async Task<List<ServerEntity>> GetAllAsync() => await this.context
        .Servers
        .AsNoTracking() // optimization ONLY for read only operations
        .ToListAsync()
        .ConfigureAwait(false); // reduces the load on SynchronizationContext

    public async Task<ServerEntity?> GetByIdAsync(Guid id) => await this.context
        .Servers
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id)
        .ConfigureAwait(false);

    public async Task<List<User>> GetServerUsersAsync(Guid serverId) => await this.context
        .Users
        .Where(s => s.ServerId == serverId)
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);

    public async Task AddAsync(ServerEntity server)
    {
        await this.context
            .Servers
            .AddAsync(server);
        await this.context.SaveChangesAsync();
    }

    public async Task UpdateServerNameAsync(Guid serverId, string? serverName)
    {
        await this
            .context
            .Servers
            .Where(u => u.Id == serverId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.ServerName, serverName));

        await this.context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await this.context
            .Users
            .Where(u => u.Id == id)
            .ExecuteDeleteAsync();
        await this.context.SaveChangesAsync();
    }
}
