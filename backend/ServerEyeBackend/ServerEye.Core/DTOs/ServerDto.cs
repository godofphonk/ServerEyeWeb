namespace ServerEye.Core.DTOs;

public class ServerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public IReadOnlyCollection<string> Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
