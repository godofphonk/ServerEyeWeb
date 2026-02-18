namespace ServerEye.Core.DTOs.Metrics;

public class MetricsSummary
{
    public double AvgCpu { get; set; }
    public double MaxCpu { get; set; }
    public double MinCpu { get; set; }
    public double AvgMemory { get; set; }
    public double MaxMemory { get; set; }
    public double MinMemory { get; set; }
    public double AvgDisk { get; set; }
    public double MaxDisk { get; set; }
    public int TotalDataPoints { get; set; }
    public TimeSpan TimeRange { get; set; }
}
