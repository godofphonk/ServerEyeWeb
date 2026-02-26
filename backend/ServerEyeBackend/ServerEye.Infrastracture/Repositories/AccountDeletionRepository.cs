namespace ServerEye.Infrastracture.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;

public sealed class AccountDeletionRepository(ServerEyeDbContext context) : IAccountDeletionRepository
{
    private readonly ServerEyeDbContext context = context;

    public async Task<List<AccountDeletion>> GetAllAsync() => await this.context
            .AccountDeletions
            .AsNoTracking()
            .ToListAsync();

    public async Task<AccountDeletion?> GetByIdAsync(Guid id) => await this.context
            .AccountDeletions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task AddAsync(AccountDeletion entity)
    {
        await this.context.AccountDeletions.AddAsync(entity);
        await this.context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await this.context.AccountDeletions.FindAsync(id);
        if (entity != null)
        {
            this.context.AccountDeletions.Remove(entity);
            await this.context.SaveChangesAsync();
        }
    }

    public async Task<AccountDeletion?> GetActiveByUserIdAsync(Guid userId)
    {
        return await this.context.AccountDeletions
            .Where(a => a.UserId == userId && !a.IsUsed && a.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<AccountDeletion?> GetByCodeAsync(string code, Guid userId)
    {
        return await this.context.AccountDeletions
            .Where(a => a.ConfirmationCode == code && a.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AccountDeletion>> GetExpiredAsync()
    {
        return await this.context.AccountDeletions
            .Where(a => !a.IsUsed && a.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task InvalidateAllByUserIdAsync(Guid userId)
    {
        var deletions = await this.context.AccountDeletions
            .Where(a => a.UserId == userId && !a.IsUsed)
            .ToListAsync();

        foreach (var deletion in deletions)
        {
            deletion.IsUsed = true;
        }

        await this.context.SaveChangesAsync();
    }
}
