namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Enums;

public interface IOAuthProvider
{
    public OAuthProvider ProviderType { get; }
    
    public Task<OAuthChallengeResponseDto> CreateChallengeAsync(string state, string codeChallenge, Uri? returnUrl);
    public Task<OAuthUserInfoDto> GetUserInfoAsync(string accessToken, string? idToken, CancellationToken cancellationToken);
    public Task<TokenResponseDto> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken);
    public bool IsEnabled();
    public Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken);
}
