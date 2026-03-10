namespace ServerEye.Core.DTOs.Auth;

public class AuthResponseDto
{
    public bool Success { get; init; } = true;
    public string Message { get; init; } = string.Empty;
    public AuthUserDto? User { get; init; }
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; set; }
}

public class RefreshTokenRequestDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthUserDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Guid ServerId { get; set; }
}
