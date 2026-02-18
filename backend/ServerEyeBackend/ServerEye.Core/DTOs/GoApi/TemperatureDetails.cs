namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class TemperatureDetails
{
    [JsonPropertyName("cpu_temperature")]
    public double CpuTemperature { get; set; }

    [JsonPropertyName("gpu_temperature")]
    public double GpuTemperature { get; set; }

    [JsonPropertyName("system_temperature")]
    public double SystemTemperature { get; set; }

    [JsonPropertyName("storage_temperatures")]
    public Dictionary<string, double> StorageTemperatures { get; set; } = new();

    [JsonPropertyName("highest_temperature")]
    public double HighestTemperature { get; set; }

    [JsonPropertyName("temperature_unit")]
    public string TemperatureUnit { get; set; } = "celsius";
}
