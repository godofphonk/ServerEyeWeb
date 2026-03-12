namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;
using ServerEye.Core.Enums;

public interface IEmailVerificationRepository : IBaseRepository<EmailVerification>
{
    public Task<EmailVerification?> GetActiveByUserIdAndTypeAsync(Guid userId, EmailVerificationType type);
    public Task<EmailVerification?> GetByCodeAsync(string code, Guid userId);
    public Task<List<EmailVerification>> GetExpiredAsync();
    public Task InvalidateAllByUserIdAsync(Guid userId, EmailVerificationType type);
}
