namespace ServerEye.Infrastracture.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;

public class UserRepository(ServerEyeDbContext context) : IUserRepository
{
    private readonly ServerEyeDbContext context = context;

    public async Task<List<User>> GetAllAsync() => await this.context
            .Users
            .AsNoTracking() // optimization ONLY for read only operations
            .ToListAsync()
            .ConfigureAwait(false); // reduces the load on SynchronizationContext

    public async Task<User?> GetByIdAsync(Guid id) => await this.context
        .Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id)
        .ConfigureAwait(false);

    public async Task<User?> GetByEmailAsync(string email) => await this.context
        .Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Email == email)
        .ConfigureAwait(false);

    public async Task<List<ServerEntity>> GetUserServersAsync(Guid userId) => await this.context
        .Servers
        .Where(s => s.UserId == userId)
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);

    public async Task AddAsync(User user)
    {
        await this.context
            .Users
            .AddAsync(user);
        await this.context.SaveChangesAsync();
    }
    public async Task UpdateUserAsync(Guid id, string? username, string? password, string? mail)
    {
        await this.context
            .Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.UserName, username)
                .SetProperty(x => x.Password, password)
                .SetProperty(x => x.Email, mail));
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

    // pagination
    public async Task<List<User>> GetUsersWithPaginationAsync(int page, int pageSize) => await this.context
        .Users
        .AsNoTracking()
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync()
        .ConfigureAwait(false);
}
