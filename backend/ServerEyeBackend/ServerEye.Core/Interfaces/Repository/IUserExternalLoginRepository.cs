namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;
using ServerEye.Core.Enums;

public interface IUserExternalLoginRepository
{
    public Task<UserExternalLogin?> GetByProviderAndProviderUserIdAsync(OAuthProvider provider, string providerUserId, CancellationToken cancellationToken = default);
    public Task<UserExternalLogin?> GetByUserIdAndProviderAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default);
    public Task<List<UserExternalLogin>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    public Task<List<UserExternalLogin>> GetByProviderAsync(OAuthProvider provider, CancellationToken cancellationToken = default);
    public Task<UserExternalLogin> AddAsync(UserExternalLogin externalLogin, CancellationToken cancellationToken = default);
    public Task<UserExternalLogin> UpdateAsync(UserExternalLogin externalLogin, CancellationToken cancellationToken = default);
    public Task DeleteAsync(UserExternalLogin externalLogin, CancellationToken cancellationToken = default);
    public Task DeleteByUserIdAndProviderAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default);
    public Task<bool> ExistsByProviderAndProviderUserIdAsync(OAuthProvider provider, string providerUserId, CancellationToken cancellationToken = default);
    public Task<bool> ExistsByUserIdAndProviderAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default);
}
