namespace ServerEye.Infrastracture.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ServerEye.Core.Entities;

public class ServerRepository(ServerEyeDbContext context)
{
    private readonly ServerEyeDbContext context = context;

    public async Task<List<ServerEntity>> GetAllServersAsync() => await this.context
        .Servers
        .AsNoTracking() // optimization ONLY for read only operations
        .ToListAsync()
        .ConfigureAwait(false); // reduces the load on SynchronizationContext

    public async Task<ServerEntity?> GetServerByKeyAsync(Guid id) => await this.context
        .Servers
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id)
        .ConfigureAwait(false);

    public async Task<List<User>> GetServerUsersAsync(Guid serverid) => await this.context
        .Users
        .Where(s => s.UserServers.Any(u => u.Id == serverid))
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);

    public async Task<EntityEntry<ServerEntity>> CreateServerAsync(ServerEntity server) => await this.context
        .Servers
        .AddAsync(server)
        .ConfigureAwait(false);
}
