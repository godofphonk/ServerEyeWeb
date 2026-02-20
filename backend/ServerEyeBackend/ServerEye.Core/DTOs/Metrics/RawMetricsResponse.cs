namespace ServerEye.Core.DTOs.Metrics;

using ServerEye.Core.DTOs.GoApi;

public class RawMetricsResponse
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Granularity { get; set; } = string.Empty;
    public List<GoApiDataPoint> DataPoints { get; set; } = new();
    public int TotalPoints { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; } // Changed from GoApiServerStatus to string
    public MetricsSummary? Summary { get; set; } // Added summary
    public bool IsCached { get; set; }
    public DateTime? CachedAt { get; set; }
    public TemperatureDetails? TemperatureDetails { get; set; }
    public NetworkDetails? NetworkDetails { get; set; }
    public DiskDetails? DiskDetails { get; set; }
}
