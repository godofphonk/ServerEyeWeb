namespace ServerEye.Core.Entities;

public class ServerEntity
{
    public Guid Id { get; init; }
    public string ServerKey { get; init; } = string.Empty;
    public string ServerId { get; init; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}
