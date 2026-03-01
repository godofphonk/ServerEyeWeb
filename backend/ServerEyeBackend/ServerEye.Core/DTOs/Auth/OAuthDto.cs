namespace ServerEye.Core.DTOs.Auth;

using ServerEye.Core.Enums;

public class OAuthLoginRequestDto
{
    public string Provider { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class OAuthLinkRequestDto
{
    public string Provider { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class OAuthUserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public Uri? AvatarUrl { get; set; }
    public bool EmailVerified { get; set; }
    public Dictionary<string, object> RawData { get; set; } = new();
}

public class OAuthProviderInfoDto
{
    public OAuthProvider Provider { get; set; }
    public string ProviderUserId { get; set; } = string.Empty;
    public string ProviderEmail { get; set; } = string.Empty;
    public string ProviderUsername { get; set; } = string.Empty;
    public Uri? ProviderAvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class OAuthChallengeResponseDto
{
    public Uri ChallengeUrl { get; set; } = new Uri("https://localhost");
    public string State { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
}

public class OAuthCallbackRequestDto
{
    public string Provider { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
}
