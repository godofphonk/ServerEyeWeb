namespace ServerEye.Core.DTOs;

public class MetricDto
{
    public string ServerId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
