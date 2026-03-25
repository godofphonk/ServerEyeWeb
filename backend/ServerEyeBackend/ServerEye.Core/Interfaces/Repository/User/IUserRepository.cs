namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;
using ServerEye.Core.Enums;

public interface IUserRepository : IBaseRepository<User>
{
    public Task<User?> GetByEmailAsync(string email);
    public Task<List<ServerEntity>> GetUserServersAsync(Guid userId);
    public Task UpdateUserAsync(User user);
    public Task<List<User>> GetByRolesAsync(IEnumerable<UserRole> roles);
}
