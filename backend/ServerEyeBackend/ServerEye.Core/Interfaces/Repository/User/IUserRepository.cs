namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;

public interface IUserRepository : IBaseRepository<User>
{
    public Task<User?> GetByEmailAsync(string email);
    public Task<List<ServerEntity>> GetUserServersAsync(Guid userId);
    public Task UpdateUserAsync(User user);
}
