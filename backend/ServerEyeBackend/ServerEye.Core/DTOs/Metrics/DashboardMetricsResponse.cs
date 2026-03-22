namespace ServerEye.Core.DTOs.Metrics;

/// <summary>
/// Dashboard metrics response for frontend consumption.
/// Contains current values and trends for dashboard display.
/// </summary>
public class DashboardMetricsResponse
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public CurrentMetrics Current { get; set; } = new();
    public MetricTrends Trends { get; set; } = new();
    public bool IsCached { get; set; }
    public DateTime? CachedAt { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Current metrics values.
/// </summary>
public class CurrentMetrics
{
    public double Cpu { get; set; }
    public double Memory { get; set; }
    public double Disk { get; set; }
    public double Network { get; set; }
    public double Temperature { get; set; }
    public double Load { get; set; }
    
    // Additional memory details
    public double MemoryCache { get; set; }
    public double MemoryBuffers { get; set; }
    public double MemoryAvailable { get; set; }
    public double MemorySwap { get; set; }
    
    // Disk I/O metrics
    public double DiskReadSpeed { get; set; }
    public double DiskWriteSpeed { get; set; }
}

/// <summary>
/// Metric trends (percentage change).
/// </summary>
public class MetricTrends
{
    public double Cpu { get; set; }
    public double Memory { get; set; }
    public double Disk { get; set; }
    public double Network { get; set; }
    public double Temperature { get; set; }
    public double Load { get; set; }
}
