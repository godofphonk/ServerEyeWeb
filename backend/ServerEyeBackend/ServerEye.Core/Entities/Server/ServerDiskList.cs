namespace ServerEye.Core.Entities.Server;

public class ServerDiskList
{
    public IReadOnlyCollection<ServerDisks> Disks { get; private set; } = [];
}
