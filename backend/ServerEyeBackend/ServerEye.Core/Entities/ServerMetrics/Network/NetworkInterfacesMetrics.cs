namespace ServerEye.Core.Entities.ServerMetrics.Network;

public class NetworkInterfacesMetrics
{
public string Name { get; set; } = string.Empty;
public long RxBytes { get; set; }
public long TxBytes { get; set; }
public long RxPackets { get; set; }
public long TxPackets { get; set; }
public double RxSpeedMbps { get; set; }
public double TxSpeedMbps { get; set; }
public string Status { get; set; } = string.Empty;
}
