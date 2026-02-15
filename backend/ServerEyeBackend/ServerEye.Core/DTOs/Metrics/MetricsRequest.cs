namespace ServerEye.Core.DTOs.Metrics;

public class MetricsRequest
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Granularity { get; set; }
}
