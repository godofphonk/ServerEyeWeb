namespace ServerEye.Core.Entities.ServerMetrics;

public class TemperatureMetrics
{
    public double CpuTemperature { get; set; }
    public double GpuTemperature { get; set; }
    public double SystemTemperature { get; set; }
    public double StorageTemperature { get; set; }
    public double HighestTemperature { get; set; }
    public string TemperatureUnit { get; set; } = string.Empty;
}
