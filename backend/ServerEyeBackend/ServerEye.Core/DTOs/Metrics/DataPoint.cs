namespace ServerEye.Core.DTOs.Metrics;

public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public MetricValue Cpu { get; set; } = new();
    public MetricValue Memory { get; set; } = new();
    public MetricValue Disk { get; set; } = new();
    public MetricValue Network { get; set; } = new();
    public MetricValue Temperature { get; set; } = new();
    public MetricValue LoadAverage { get; set; } = new();
    public int SampleCount { get; set; }
}
