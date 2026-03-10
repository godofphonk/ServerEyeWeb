namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;

public interface IOAuthService
{
    // OAuth flow management
    public Task<OAuthChallengeResponseDto> CreateChallengeAsync(OAuthProvider provider, Uri? returnUrl = null, string? action = null, CancellationToken cancellationToken = default);
    public Task<AuthResponseDto> ProcessCallbackAsync(OAuthCallbackRequestDto request, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    
    // External login management
    public Task<List<OAuthProviderInfoDto>> GetUserExternalLoginsAsync(Guid userId, CancellationToken cancellationToken = default);
    public Task<AuthResponseDto> LinkExternalLoginAsync(Guid userId, OAuthLinkRequestDto request, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    public Task UnlinkExternalLoginAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default);
    
    // User information
    public Task<OAuthUserInfoDto> GetUserInfoAsync(OAuthProvider provider, string accessToken, string? idToken = null, CancellationToken cancellationToken = default);
    public Task<bool> ValidateAccessTokenAsync(OAuthProvider provider, string accessToken, CancellationToken cancellationToken = default);
    
    // Provider validation
    public bool IsProviderEnabled(OAuthProvider provider);
    public OAuthProvider ParseProvider(string providerName);
    
    // Account linking/unlinking
    public Task<bool> CanLinkAccountAsync(Guid userId, OAuthProvider provider, string providerUserId, CancellationToken cancellationToken = default);
    public Task<User?> FindUserByExternalLoginAsync(OAuthProvider provider, string providerUserId, CancellationToken cancellationToken = default);
    public Task<User> CreateOrUpdateUserFromExternalLoginAsync(OAuthProvider provider, OAuthUserInfoDto userInfo, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
}
