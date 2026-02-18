namespace ServerEye.Core.Entities;

using ServerEye.Core.Enums;

public class UserServerAccess
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ServerId { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public DateTime AddedAt { get; set; }

    public User User { get; set; } = null!;
    public Server Server { get; set; } = null!;
}
