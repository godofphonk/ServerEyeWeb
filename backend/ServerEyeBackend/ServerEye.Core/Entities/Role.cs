namespace ServerEye.Core.Entities;

using ServerEye.Core.Enums;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RoleType Type { get; set; }
}
