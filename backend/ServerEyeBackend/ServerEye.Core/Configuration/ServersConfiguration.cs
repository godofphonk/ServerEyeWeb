namespace ServerEye.Core.Configuration;

public class ServersConfiguration
{
    public bool UseMockData { get; init; }
    public int MockDataDelayMs { get; init; } = 1;
    public bool EnableDetailedLogging { get; init; } = true;
    public int MaxServersPerUser { get; init; } = 100;
}
