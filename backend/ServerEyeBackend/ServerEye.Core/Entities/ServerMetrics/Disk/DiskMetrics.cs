namespace ServerEye.Core.Entities.ServerMetrics.Disk;

public class DiskMetrics
{
    public double TotalGb { get; set; }
    public double UsedGb { get; set; }
    public double FreeGb { get; set; }
    public double UsedPercent { get; set; }
    public string Path { get; set; } = string.Empty;
    public string FileSystem { get; set; } = string.Empty;
}
