namespace ServerEye.Core.Entities.ServerMetrics;

public class CpuMetrics
{
    public double UsageTotal { get; set; }
    public double UsageUser { get; set; }
    public double UsageSystem { get; set; }
    public double UsageIdle { get; set; }
    public double CpuTemp { get; set; }
    public int Cores { get; set; }
    public double Frequency { get; set; }
}
