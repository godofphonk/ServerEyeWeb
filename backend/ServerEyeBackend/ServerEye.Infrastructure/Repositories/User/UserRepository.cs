namespace ServerEye.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;

public sealed class UserRepository(ServerEyeDbContext context) : IUserRepository
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
    public async Task UpdateUserAsync(User user)
    {
        await this.context
            .Users
            .Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.UserName, user.UserName)
                .SetProperty(x => x.Password, user.Password)
                .SetProperty(x => x.Email, user.Email)
                .SetProperty(x => x.IsEmailVerified, user.IsEmailVerified)
                .SetProperty(x => x.EmailVerifiedAt, user.EmailVerifiedAt)
                .SetProperty(x => x.PendingEmail, user.PendingEmail));

        // ExecuteUpdateAsync already saves changes to database, no need for SaveChangesAsync()
    }
    public async Task DeleteAsync(Guid id)
    {
        await this.context
            .Users
            .Where(u => u.Id == id)
            .ExecuteDeleteAsync();

        // ExecuteDeleteAsync already saves changes to database, no need for SaveChangesAsync()
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
