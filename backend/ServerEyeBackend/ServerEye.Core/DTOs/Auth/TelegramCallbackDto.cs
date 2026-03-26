namespace ServerEye.Core.DTOs.Auth;

public class TelegramCallbackRequestDto
{
    public TelegramUserDataDto? UserData { get; set; }
    public string State { get; set; } = string.Empty;
    public string? Action { get; set; }
    public bool LinkingAction { get; set; }
    public string? UserId { get; set; }
}

