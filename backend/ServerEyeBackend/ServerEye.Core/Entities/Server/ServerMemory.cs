namespace ServerEye.Core.Entities.Server;

public class ServerMemory
{
    public decimal TotalGb { get; set; }
    public decimal UsedGb { get; set; }
    public decimal AvailableGb { get; set; }
    public decimal FreeGb { get; set; }
    public decimal BuffersGb { get; set; }
    public decimal CachedGb { get; set; }
    public decimal UsedPercent { get; set; }
}
