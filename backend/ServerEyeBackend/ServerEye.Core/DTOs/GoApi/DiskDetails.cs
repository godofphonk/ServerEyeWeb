namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class DiskDetails
{
    [JsonPropertyName("disks")]
    public List<DiskInfo> Disks { get; set; } = new();
}

public class DiskInfo
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("free_gb")]
    public double FreeGb { get; set; }

    [JsonPropertyName("used_gb")]
    public double UsedGb { get; set; }

    [JsonPropertyName("total_gb")]
    public double TotalGb { get; set; }

    [JsonPropertyName("filesystem")]
    public string Filesystem { get; set; } = string.Empty;

    [JsonPropertyName("used_percent")]
    public double UsedPercent { get; set; }
}
