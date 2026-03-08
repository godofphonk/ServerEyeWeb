namespace ServerEye.Core.DTOs.Auth;

public class TelegramCallbackRequestDto
{
    public TelegramUserDataDto? UserData { get; set; }
    public string State { get; set; } = string.Empty;
}

