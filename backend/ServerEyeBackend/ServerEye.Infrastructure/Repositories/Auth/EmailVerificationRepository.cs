namespace ServerEye.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;

public sealed class EmailVerificationRepository(ServerEyeDbContext context) : IEmailVerificationRepository
{
    private readonly ServerEyeDbContext context = context;

    public async Task<List<EmailVerification>> GetAllAsync() => await this.context
            .EmailVerifications
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task<EmailVerification?> GetByIdAsync(Guid id) => await this.context
            .EmailVerifications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            .ConfigureAwait(false);

    public async Task<EmailVerification?> GetActiveByUserIdAndTypeAsync(Guid userId, EmailVerificationType type) => await this.context
            .EmailVerifications
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Type == type && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

    public async Task<EmailVerification?> GetByCodeAsync(string code, Guid userId) => await this.context
            .EmailVerifications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code && x.UserId == userId && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
            .ConfigureAwait(false);

    public async Task<List<EmailVerification>> GetExpiredAsync() => await this.context
            .EmailVerifications
            .Where(x => x.ExpiresAt <= DateTime.UtcNow && !x.IsUsed)
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task InvalidateAllByUserIdAsync(Guid userId, EmailVerificationType type)
    {
        var verifications = await this.context.EmailVerifications
            .Where(x => x.UserId == userId && x.Type == type && !x.IsUsed)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var verification in verifications)
        {
            verification.IsUsed = true;
        }

        await this.context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddAsync(EmailVerification entity)
    {
        await this.context.EmailVerifications.AddAsync(entity);
        await this.context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await this.GetByIdAsync(id);
        if (entity != null)
        {
            this.context.EmailVerifications.Remove(entity);
            await this.context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
