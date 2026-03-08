namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class NetworkDetails
{
    [JsonPropertyName("interfaces")]
    public Dictionary<string, NetworkInterface> Interfaces { get; init; } = new();

    [JsonPropertyName("total_rx")]
    public long TotalRx { get; init; }

    [JsonPropertyName("total_tx")]
    public long TotalTx { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }
}

public class NetworkInterface
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("rx_bytes")]
    public long RxBytes { get; init; }

    [JsonPropertyName("tx_bytes")]
    public long TxBytes { get; init; }

    [JsonPropertyName("rx_packets")]
    public long RxPackets { get; init; }

    [JsonPropertyName("tx_packets")]
    public long TxPackets { get; init; }

    [JsonPropertyName("speed")]
    public long Speed { get; init; }

    [JsonPropertyName("duplex")]
    public string Duplex { get; init; } = string.Empty;

    [JsonPropertyName("mtu")]
    public int Mtu { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}
