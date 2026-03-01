namespace ServerEye.Infrastracture.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Infrastracture;

public sealed class UserExternalLoginRepository(ServerEyeDbContext dbContext) : IUserExternalLoginRepository
{
    private readonly ServerEyeDbContext dbContext = dbContext;

    public async Task<UserExternalLogin?> GetByProviderAndProviderUserIdAsync(OAuthProvider provider, string providerUserId, CancellationToken cancellationToken = default)
    {
        return await this.dbContext.UserExternalLogins
            .Include(el => el.User)
            .FirstOrDefaultAsync(el => el.Provider == provider && el.ProviderUserId == providerUserId, cancellationToken);
    }

    public async Task<UserExternalLogin?> GetByUserIdAndProviderAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default)
    {
        return await this.dbContext.UserExternalLogins
            .Include(el => el.User)
            .FirstOrDefaultAsync(el => el.UserId == userId && el.Provider == provider, cancellationToken);
    }

    public async Task<List<UserExternalLogin>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await this.dbContext.UserExternalLogins
            .Include(el => el.User)
            .Where(el => el.UserId == userId)
            .OrderByDescending(el => el.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserExternalLogin>> GetByProviderAsync(OAuthProvider provider, CancellationToken cancellationToken = default)
    {
        return await this.dbContext.UserExternalLogins
            .Include(el => el.User)
            .Where(el => el.Provider == provider)
            .OrderByDescending(el => el.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserExternalLogin> AddAsync(UserExternalLogin externalLogin, CancellationToken cancellationToken = default)
    {
        await this.dbContext.UserExternalLogins.AddAsync(externalLogin, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        return externalLogin;
    }

    public async Task<UserExternalLogin> UpdateAsync(UserExternalLogin externalLogin, CancellationToken cancellationToken = default)
    {
        this.dbContext.UserExternalLogins.Update(externalLogin);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        return externalLogin;
    }

    public async Task DeleteAsync(UserExternalLogin externalLogin, CancellationToken cancellationToken = default)
    {
        this.dbContext.UserExternalLogins.Remove(externalLogin);
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByUserIdAndProviderAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default)
    {
        var externalLogin = await this.GetByUserIdAndProviderAsync(userId, provider, cancellationToken);
        if (externalLogin != null)
        {
            await this.DeleteAsync(externalLogin, cancellationToken);
        }
    }

    public async Task<bool> ExistsByProviderAndProviderUserIdAsync(OAuthProvider provider, string providerUserId, CancellationToken cancellationToken = default)
    {
        return await this.dbContext.UserExternalLogins
            .AnyAsync(el => el.Provider == provider && el.ProviderUserId == providerUserId, cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAndProviderAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default)
    {
        return await this.dbContext.UserExternalLogins
            .AnyAsync(el => el.UserId == userId && el.Provider == provider, cancellationToken);
    }
}
