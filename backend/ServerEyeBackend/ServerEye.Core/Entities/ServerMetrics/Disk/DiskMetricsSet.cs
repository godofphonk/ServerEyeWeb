namespace ServerEye.Core.Entities.ServerMetrics.Disk;

public class DiskMetricsSet
{
    public IReadOnlyCollection<DiskMetrics> Disks { get; set; } = [];
}
