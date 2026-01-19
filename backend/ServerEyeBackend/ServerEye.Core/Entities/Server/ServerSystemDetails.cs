namespace ServerEye.Core.Entities.Server;

public class ServerSystemDetails
{
    public string Hostname { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
    public string Kernel { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public long UptimeSeconds { get; set; }
    public string UptimeHuman { get; set; } = string.Empty;
    public DateTime BootTime { get; set; }
    public int ProcessesTotal { get; set; }
    public int ProcessesRunning { get; set; }
    public int ProcessesSleeping { get; set; }
}
