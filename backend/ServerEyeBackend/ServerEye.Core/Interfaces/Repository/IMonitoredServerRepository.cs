namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;

public interface IMonitoredServerRepository
{
    public Task<Server?> GetByServerIdAsync(string serverId);
    public Task<Server?> GetByIdAsync(Guid id);
    public Task AddAsync(Server server);
    public Task UpdateAsync(Server server);
    public Task DeleteAsync(Guid id);
    public Task<bool> ExistsAsync(string serverId);
    public Task<List<Server>> GetAllAsync();
}
