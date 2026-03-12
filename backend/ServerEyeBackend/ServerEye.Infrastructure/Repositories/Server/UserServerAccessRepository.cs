namespace ServerEye.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;

public sealed class UserServerAccessRepository : IUserServerAccessRepository
{
    private readonly ServerEyeDbContext context;

    public UserServerAccessRepository(ServerEyeDbContext context) => this.context = context;

    public async Task<List<Server>> GetUserServersAsync(Guid userId)
    {
        return await this.context.UserServerAccesses
            .Where(a => a.UserId == userId)
            .Include(a => a.Server)
            .Select(a => a.Server)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<List<User>> GetServerUsersAsync(string serverId)
    {
        var server = await this.context.MonitoredServers
            .FirstOrDefaultAsync(s => s.ServerId == serverId);

        if (server == null)
        {
            return new List<User>();
        }

        return await this.context.UserServerAccesses
            .Where(a => a.ServerId == server.Id)
            .Include(a => a.User)
            .Select(a => a.User)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<bool> HasAccessAsync(Guid userId, string serverId)
    {
        var server = await this.context.MonitoredServers
            .FirstOrDefaultAsync(s => s.ServerId == serverId);

        if (server == null)
        {
            return false;
        }

        return await this.context.UserServerAccesses
            .AnyAsync(a => a.UserId == userId && a.ServerId == server.Id)
            .ConfigureAwait(false);
    }

    public async Task<AccessLevel?> GetAccessLevelAsync(Guid userId, string serverId)
    {
        var server = await this.context.MonitoredServers
            .FirstOrDefaultAsync(s => s.ServerId == serverId);

        if (server == null)
        {
            return null;
        }

        var access = await this.context.UserServerAccesses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.ServerId == server.Id)
            .ConfigureAwait(false);

        return access?.AccessLevel;
    }

    public async Task AddAccessAsync(UserServerAccess access)
    {
        await this.context.UserServerAccesses.AddAsync(access);
        await this.context.SaveChangesAsync();
    }

    public async Task RemoveAccessAsync(Guid userId, string serverId)
    {
        var server = await this.context.MonitoredServers
            .FirstOrDefaultAsync(s => s.ServerId == serverId);

        if (server == null)
        {
            return;
        }

        await this.context.UserServerAccesses
            .Where(a => a.UserId == userId && a.ServerId == server.Id)
            .ExecuteDeleteAsync();
    }

    public async Task UpdateAccessLevelAsync(Guid userId, string serverId, AccessLevel level)
    {
        var server = await this.context.MonitoredServers
            .FirstOrDefaultAsync(s => s.ServerId == serverId);

        if (server == null)
        {
            return;
        }

        await this.context.UserServerAccesses
            .Where(a => a.UserId == userId && a.ServerId == server.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.AccessLevel, level));
    }
}
