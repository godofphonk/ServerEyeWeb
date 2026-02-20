namespace ServerEye.Core.DTOs.Server;

using ServerEye.Core.Enums;

public class ServerResponse
{
    public Guid Id { get; set; }
    public string ServerId { get; set; } = string.Empty;
    public string ServerKey { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsActive { get; set; }
}
