namespace ServerEye.Core.Entities.Server;

public class ServerCpu
{
    public decimal UsageTotal { get; set; }
    public decimal UsageUser { get; set; }
    public decimal UsageSystem { get; set; }
    public decimal UsageIdle { get; set; }
    public decimal Load1Min { get; set; }
    public decimal Load5Min { get; set; }
    public decimal Load15Min { get; set; }
    public int Cores { get; set; }
    public int Frequency { get; set; }
}
