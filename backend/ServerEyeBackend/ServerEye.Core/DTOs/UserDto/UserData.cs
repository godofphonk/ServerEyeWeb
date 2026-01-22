namespace ServerEye.Core.DTOs.UserDto;

public class UserData
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? ServerId { get; set; }
}
