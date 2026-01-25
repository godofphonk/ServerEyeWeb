namespace ServerEye.Core.Entities.ServerMetrics;

public class MemoryMetrics
{
    public double TotalGb { get; set; }
    public double UsedGb { get; set; }
    public double AvailableGb { get; set; }
    public double FreeGb { get; set; }
    public double BuffersGb { get; set; }
    public double CachedGb { get; set; }
}
