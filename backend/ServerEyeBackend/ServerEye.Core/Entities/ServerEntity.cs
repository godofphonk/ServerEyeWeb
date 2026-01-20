namespace ServerEye.Core.Entities;

public class ServerEntity
{
    public Guid Id { get; init; }
    public string ServerKey { get; init; } = string.Empty;
    public string ServerId { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IReadOnlyCollection<User> User { get; set; } = [];
}
