namespace ServerEye.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities.Billing;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options)
    {
    }

    public DbSet<SubscriptionPlanEntity> SubscriptionPlans { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<WebhookEvent> WebhookEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SubscriptionPlanEntity configuration
        modelBuilder.Entity<SubscriptionPlanEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlanType).HasConversion<string>();
            entity.Property(e => e.Features).HasColumnType("jsonb");
            entity.HasIndex(e => e.PlanType).IsUnique();
        });

        // Configure Features as Dictionary<string, string> to jsonb
        modelBuilder.Entity<SubscriptionPlanEntity>()
            .Property(e => e.Features)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

        // Subscription configuration
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.PlanType).HasConversion<string>();
            entity.Property(e => e.Provider).HasConversion<string>();
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Provider).HasConversion<string>();
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasOne<Subscription>()
                .WithMany()
                .HasForeignKey(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // WebhookEvent configuration
        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).HasConversion<string>();
            entity.Property(e => e.Payload).HasColumnType("jsonb");
            entity.HasIndex(e => e.EventId).IsUnique();
        });
    }
}
