namespace ServerEye.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
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
        await this.context.RefreshTokens.AddAsync(entity);
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
        RefreshToken? entity = await this.context.RefreshTokens.FindAsync(id).ConfigureAwait(false);
        if (entity != null)
        {
            this.context.RefreshTokens.Remove(entity);
            await this.context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        var tokens = await this.context.RefreshTokens
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await this.context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task RevokeTokenAsync(Guid tokenId)
    {
        var token = await this.context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Id == tokenId)
            .ConfigureAwait(false);

        if (token != null)
        {
            token.IsRevoked = true;
            await this.context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
