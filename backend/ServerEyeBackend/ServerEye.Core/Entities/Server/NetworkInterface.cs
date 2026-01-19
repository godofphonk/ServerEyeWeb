namespace ServerEye.Core.Entities.Server;

public class NetworkInterface
{
    public string Name { get; set; } = string.Empty;
    public long RxBytes { get; set; }
    public long TxBytes { get; set; }
    public long RxPackets { get; set; }
    public long TxPackets { get; set; }
    public decimal RxSpeedMbps { get; set; }
    public decimal TxSpeedMbps { get; set; }
    public string Status { get; set; } = string.Empty;
}
