using System.Text.Json.Serialization;

namespace ServerEye.Core.DTOs.Auth;

public class AuthResponseDto
{
    public bool Success { get; init; } = true;
    public string Message { get; init; } = string.Empty;
    public AuthUserDto? User { get; init; }
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; set; }
    
    [JsonPropertyName("skipEmailVerification")]
    public bool SkipEmailVerification { get; set; }
}

public class RefreshTokenRequestDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthUserDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }
    
    [JsonPropertyName("username")]
    public string UserName { get; init; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
    
    [JsonPropertyName("serverId")]
    public Guid ServerId { get; set; }
    
    [JsonPropertyName("isEmailVerified")]
    public bool IsEmailVerified { get; init; }
    
    [JsonPropertyName("requiresEmailVerification")]
    public bool RequiresEmailVerification { get; init; }
}
