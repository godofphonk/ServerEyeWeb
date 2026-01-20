namespace ServerEye.Infrastracture.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;

public class UserRepository(ServerEyeDbContext context)
{
    private readonly ServerEyeDbContext context = context;

    public async Task<List<User>> GetAllUsersAsync() => await this.context
            .Users
            .AsNoTracking() // optimization ONLY for read only operations
            .ToListAsync()
            .ConfigureAwait(false); // reduces the load on SynchronizationContext

    public async Task<User?> GetUserByIdAsync(Guid id) => await this.context
        .Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id)
        .ConfigureAwait(false);

    public async Task<User?> GetUserByEmailAsync(string email) => await this.context
        .Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Email == email)
        .ConfigureAwait(false);

    public async Task<List<ServerEntity>> GetUserServersAsync(Guid userId) => await this.context
        .Servers
        .Where(s => s.User.Any(u => u.Id == userId))
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);
}
