namespace ServerEye.Core.Entities.Server;

public class ServerTemperatures
{
    public decimal CpuTemperature { get; set; }
    public decimal GpuTemperature { get; set; }
    public decimal SystemTemperature { get; set; }
    public decimal? StorageTemperatures { get; set; }
    public decimal HighestTemperature { get; set; }
    public string TemperatureUnit { get; set; } = string.Empty;
}
