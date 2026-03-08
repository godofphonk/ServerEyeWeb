namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class DiskDetails
{
    [JsonPropertyName("disks")]
    public List<DiskInfo> Disks { get; init; } = new();
}

public class DiskInfo
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("free_gb")]
    public double FreeGb { get; init; }

    [JsonPropertyName("used_gb")]
    public double UsedGb { get; init; }

    [JsonPropertyName("total_gb")]
    public double TotalGb { get; init; }

    [JsonPropertyName("filesystem")]
    public string Filesystem { get; init; } = string.Empty;

    [JsonPropertyName("used_percent")]
    public double UsedPercent { get; init; }
}
