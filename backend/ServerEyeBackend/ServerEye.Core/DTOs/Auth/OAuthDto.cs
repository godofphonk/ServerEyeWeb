using System.Text.Json.Serialization;
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
    public string Provider { get; init; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
}

public class OAuthUserInfoDto
{
    public string Id { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public Uri? AvatarUrl { get; init; }
    public bool EmailVerified { get; init; }
    public Dictionary<string, object> RawData { get; init; } = new();
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
    public Uri ChallengeUrl { get; init; } = new Uri("https://localhost");
    public string State { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
}

public class OAuthCallbackRequestDto
{
    public string Provider { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
    public bool LinkingAction { get; set; }
    public string? UserId { get; set; }
}

public class TokenResponseDto
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("id_token")]
    public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;
}
