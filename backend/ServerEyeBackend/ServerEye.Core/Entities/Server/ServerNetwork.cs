namespace ServerEye.Core.Entities.Server;

public class ServerNetwork
{
    public IReadOnlyCollection<NetworkInterface> Interfaces { get; private set; } = [];
    public decimal TotalRxMbps { get; set; }
    public decimal TotalTxMbps { get; set; }
}
