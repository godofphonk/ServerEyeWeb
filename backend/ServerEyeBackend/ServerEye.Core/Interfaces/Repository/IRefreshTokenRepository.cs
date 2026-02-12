namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;

public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
{
    public Task<RefreshToken?> GetByTokenAsync(string token);
    public Task<RefreshToken?> GetByUserIdAsync(Guid userId);
    public Task RevokeAllUserTokensAsync(Guid userId);
    public Task RevokeTokenAsync(Guid tokenId);
}
