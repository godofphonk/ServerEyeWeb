namespace ServerEye.Core.Entities;

public class Server
{
    public Guid Id { get; set; }
    public string ServerId { get; set; } = string.Empty;
    public string ServerKey { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserServerAccess> UserAccesses { get; set; } = new List<UserServerAccess>();
}
