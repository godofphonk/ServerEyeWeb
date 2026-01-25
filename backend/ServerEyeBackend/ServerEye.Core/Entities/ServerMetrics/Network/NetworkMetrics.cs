namespace ServerEye.Core.Entities.ServerMetrics.Network;

public class NetworkMetrics
{
    // ReSharper disable once CollectionNeverUpdated.Local
    private readonly List<NetworkInterfacesMetrics> interfaces = [];

    public IReadOnlyCollection<NetworkInterfacesMetrics> Interfaces => this.interfaces;
    public double TotalRxMbps { get; set; }
    public double TotalTxMbps { get; set; }
}
