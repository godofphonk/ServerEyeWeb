namespace ServerEye.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;

public sealed class RefreshTokenRepository(ServerEyeDbContext context) : IRefreshTokenRepository
{
    private readonly ServerEyeDbContext context = context;

    public async Task<List<RefreshToken>> GetAllAsync() => await this.context
            .RefreshTokens
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task<RefreshToken?> GetByIdAsync(Guid id) => await this.context
            .RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            .ConfigureAwait(false);

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        return await this.context
            .RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow)
            .ConfigureAwait(false);
    }

    public async Task<RefreshToken?> GetByUserIdAsync(Guid userId) => await this.context
            .RefreshTokens
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

    public async Task AddAsync(RefreshToken entity)
    {
        EntityEntry<RefreshToken> entityEntry = await this.context.RefreshTokens.AddAsync(entity);
        await this.context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<RefreshToken> UpdateAsync(RefreshToken entity)
    {
        this.context.RefreshTokens.Update(entity);
        await this.context.SaveChangesAsync().ConfigureAwait(false);
        return entity;
    }

    public async Task DeleteAsync(Guid id)
    {
        RefreshToken? entity = await this.GetByIdAsync(id);
        if (entity != null)
        {
            this.context.RefreshTokens.Remove(entity);
            await this.context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        await this.context.RefreshTokens
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true))
            .ConfigureAwait(false);

        // ExecuteUpdateAsync already saves changes to database, no need for SaveChangesAsync()
    }

    public async Task RevokeTokenAsync(Guid tokenId)
    {
        await this.context.RefreshTokens
            .Where(x => x.Id == tokenId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true))
            .ConfigureAwait(false);

        // ExecuteUpdateAsync already saves changes to database, no need for SaveChangesAsync()
    }
}
