namespace ServerEye.Core.DTOs.Metrics;

public class MetricsResponse
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Granularity { get; set; } = string.Empty;
    public List<DataPoint> DataPoints { get; set; } = new();
    public int TotalPoints { get; set; }
    public MetricsSummary Summary { get; set; } = new();
    public string? Message { get; set; }
    public bool IsCached { get; set; }
    public DateTime? CachedAt { get; set; }
}
