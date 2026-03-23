namespace ServerEye.Core.Entities.Billing;

using System.ComponentModel.DataAnnotations.Schema;
using ServerEye.Core.Enums;

public class Subscription
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("userid")]
    public Guid UserId { get; set; }

    [Column("planid")]
    public Guid PlanId { get; set; }

    [Column("status")]
    public SubscriptionStatus Status { get; set; }

    [Column("currentperiodstart")]
    public DateTime? CurrentPeriodStart { get; set; }

    [Column("currentperiodend")]
    public DateTime? CurrentPeriodEnd { get; set; }

    [Column("cancelatperiodend")]
    public bool CancelAtPeriodEnd { get; set; }

    [Column("createdat")]
    public DateTime CreatedAt { get; set; }

    [Column("updatedat")]
    public DateTime UpdatedAt { get; set; }
}
