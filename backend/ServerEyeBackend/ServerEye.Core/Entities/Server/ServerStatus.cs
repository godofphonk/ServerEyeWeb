namespace ServerEye.Core.Entities.Server;

public class ServerStatus
{
    public string AgentVersion { get; set; } = string.Empty;

    public string Hostname { get; set; } = string.Empty;

    public DateTime LastSeen { get; set; }

    public bool Online { get; set; }

    public string OsInfo { get; set; } = string.Empty;
}
