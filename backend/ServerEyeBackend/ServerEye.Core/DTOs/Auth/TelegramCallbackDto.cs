namespace ServerEye.Core.DTOs.Auth;

public class TelegramCallbackRequestDto
{
    public TelegramUserDataDto? UserData { get; set; }
    public string State { get; set; } = string.Empty;
}

public class TelegramUserDataDto
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public long AuthDate { get; set; }
    public string Hash { get; set; } = string.Empty;
}
