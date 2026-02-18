namespace ServerEye.Core.DTOs.Server;

using ServerEye.Core.Enums;

public class ShareServerRequest
{
    public string ServerId { get; set; } = string.Empty;
    public string TargetUserEmail { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; }
}
