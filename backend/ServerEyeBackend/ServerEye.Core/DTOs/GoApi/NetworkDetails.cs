namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class NetworkDetails
{
    [JsonPropertyName("interfaces")]
    public List<NetworkInterface> Interfaces { get; set; } = new();

    [JsonPropertyName("total_rx")]
    public long TotalRx { get; set; }

    [JsonPropertyName("total_tx")]
    public long TotalTx { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class NetworkInterface
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rx_bytes")]
    public long RxBytes { get; set; }

    [JsonPropertyName("tx_bytes")]
    public long TxBytes { get; set; }

    [JsonPropertyName("rx_packets")]
    public long RxPackets { get; set; }

    [JsonPropertyName("tx_packets")]
    public long TxPackets { get; set; }

    [JsonPropertyName("speed")]
    public long Speed { get; set; }

    [JsonPropertyName("duplex")]
    public string Duplex { get; set; } = string.Empty;

    [JsonPropertyName("mtu")]
    public int Mtu { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
