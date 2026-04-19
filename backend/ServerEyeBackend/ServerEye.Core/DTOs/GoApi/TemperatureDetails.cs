namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class TemperatureDetails
{
    [JsonPropertyName("cpu_temperature")]
    public double CpuTemperature { get; init; }

    [JsonPropertyName("gpu_temperature")]
    public double GpuTemperature { get; init; }

    [JsonPropertyName("system_temperature")]
    public double SystemTemperature { get; init; }

    [JsonPropertyName("storage_temperatures")]
    public Dictionary<string, double> StorageTemperatures { get; init; } = new();

    [JsonPropertyName("storage")]
    public List<StorageTemperature>? Storage { get; init; }

    [JsonPropertyName("highest_temperature")]
    public double HighestTemperature { get; init; }

    [JsonPropertyName("temperature_unit")]
    public string TemperatureUnit { get; init; } = "celsius";
}

public class StorageTemperature
{
    [JsonPropertyName("device")]
    public string Device { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }
}
