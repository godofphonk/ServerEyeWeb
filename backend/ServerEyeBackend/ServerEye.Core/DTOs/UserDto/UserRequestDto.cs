namespace ServerEye.Core.DTOs.UserDto;

public class UserRequestDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid ServerId { get; set; }
}
