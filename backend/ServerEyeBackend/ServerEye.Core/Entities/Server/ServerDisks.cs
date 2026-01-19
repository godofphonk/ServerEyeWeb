namespace ServerEye.Core.Entities.Server;

public class ServerDisks
{
    public string Path { get; set; } = string.Empty;
    public decimal TotalGb { get; set; }
    public decimal UsedGb { get; set; }
    public decimal FreeGb { get; set; }
    public decimal UsedPercent { get; set; }
    public string Filesystem { get; set; } = string.Empty;
}
