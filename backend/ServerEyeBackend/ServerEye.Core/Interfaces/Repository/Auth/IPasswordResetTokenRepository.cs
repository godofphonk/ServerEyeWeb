namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;

public interface IPasswordResetTokenRepository : IBaseRepository<PasswordResetToken>
{
    public Task<PasswordResetToken?> GetActiveByTokenAsync(string token);
    public Task<PasswordResetToken?> GetActiveByUserIdAsync(Guid userId);
    public Task<List<PasswordResetToken>> GetExpiredAsync();
    public Task InvalidateAllByUserIdAsync(Guid userId);
}
