namespace ServerEye.Core.Entities.Billing;

using ServerEye.Core.Enums;

public class SubscriptionPlanEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string[] Features { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
