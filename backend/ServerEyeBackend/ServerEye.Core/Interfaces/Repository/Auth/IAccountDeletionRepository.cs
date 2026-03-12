namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;

public interface IAccountDeletionRepository : IBaseRepository<AccountDeletion>
{
    public Task<AccountDeletion?> GetActiveByUserIdAsync(Guid userId);
    public Task<AccountDeletion?> GetByCodeAsync(string code, Guid userId);
    public Task<List<AccountDeletion>> GetExpiredAsync();
    public Task InvalidateAllByUserIdAsync(Guid userId);
}
