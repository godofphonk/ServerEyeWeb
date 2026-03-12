namespace ServerEye.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;

public sealed class PasswordResetTokenRepository(ServerEyeDbContext context) : IPasswordResetTokenRepository
{
    private readonly ServerEyeDbContext context = context;

    public async Task<List<PasswordResetToken>> GetAllAsync() => await this.context
            .PasswordResetTokens
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task<PasswordResetToken?> GetByIdAsync(Guid id) => await this.context
            .PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            .ConfigureAwait(false);

    public async Task<PasswordResetToken?> GetActiveByTokenAsync(string token) => await this.context
            .PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
            .ConfigureAwait(false);

    public async Task<PasswordResetToken?> GetActiveByUserIdAsync(Guid userId) => await this.context
            .PasswordResetTokens
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

    public async Task<List<PasswordResetToken>> GetExpiredAsync() => await this.context
            .PasswordResetTokens
            .Where(x => x.ExpiresAt <= DateTime.UtcNow && !x.IsUsed)
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task InvalidateAllByUserIdAsync(Guid userId)
    {
        var tokens = await this.context.PasswordResetTokens
            .Where(x => x.UserId == userId && !x.IsUsed)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.IsUsed = true;
        }

        await this.context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddAsync(PasswordResetToken entity)
    {
        await this.context.PasswordResetTokens.AddAsync(entity);
        await this.context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await this.GetByIdAsync(id);
        if (entity != null)
        {
            this.context.PasswordResetTokens.Remove(entity);
            await this.context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
