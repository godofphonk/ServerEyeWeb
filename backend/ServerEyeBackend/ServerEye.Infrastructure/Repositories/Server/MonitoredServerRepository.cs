namespace ServerEye.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;

public sealed class MonitoredServerRepository : IMonitoredServerRepository
{
    private readonly ServerEyeDbContext context;

    public MonitoredServerRepository(ServerEyeDbContext context) => this.context = context;

    public async Task<Server?> GetByServerIdAsync(string serverId)
    {
        return await this.context.MonitoredServers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ServerId == serverId)
            .ConfigureAwait(false);
    }

    public async Task<Server?> GetByIdAsync(Guid id)
    {
        return await this.context.MonitoredServers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Server server)
    {
        await this.context.MonitoredServers.AddAsync(server);
        await this.context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Server server)
    {
        this.context.MonitoredServers.Update(server);
        await this.context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await this.context.MonitoredServers
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync();
    }

    public async Task<bool> ExistsAsync(string serverId)
    {
        return await this.context.MonitoredServers
            .AnyAsync(s => s.ServerId == serverId)
            .ConfigureAwait(false);
    }

    public async Task<List<Server>> GetAllAsync()
    {
        return await this.context.MonitoredServers
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
