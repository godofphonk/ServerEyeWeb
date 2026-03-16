namespace ServerEye.Infrastructure;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Entities.Billing;

public sealed class ServerEyeDbContext : DbContext
{
    public ServerEyeDbContext(DbContextOptions<ServerEyeDbContext> options)
        : base(options)
    {
    }
    public DbSet<User> Users => this.Set<User>();
    public DbSet<ServerEntity> Servers => this.Set<ServerEntity>();
    public DbSet<RefreshToken> RefreshTokens => this.Set<RefreshToken>();
    public DbSet<Server> MonitoredServers => this.Set<Server>();
    public DbSet<UserServerAccess> UserServerAccesses => this.Set<UserServerAccess>();
    public DbSet<EmailVerification> EmailVerifications => this.Set<EmailVerification>();
    public DbSet<PasswordResetToken> PasswordResetTokens => this.Set<PasswordResetToken>();
    public DbSet<AccountDeletion> AccountDeletions => this.Set<AccountDeletion>();
    public DbSet<UserExternalLogin> UserExternalLogins => this.Set<UserExternalLogin>();
    public DbSet<SubscriptionPlanEntity> SubscriptionPlans => this.Set<SubscriptionPlanEntity>();
    public DbSet<Subscription> Subscriptions => this.Set<Subscription>();
    public DbSet<Payment> Payments => this.Set<Payment>();
    public DbSet<WebhookEvent> WebhookEvents => this.Set<WebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder?.Entity<User>(entity =>
        {
            entity.Property(u => u.Role).HasConversion<int>();
        });

        modelBuilder?.Entity<EmailVerification>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Code, e.IsUsed });
            entity.Property(e => e.Type).HasConversion<int>();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder?.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.Token, p.IsUsed });
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder?.Entity<ServerEntity>()
            .HasIndex(s => s.ServerKey)
            .IsUnique();

        modelBuilder?.Entity<Server>()
            .HasIndex(s => s.ServerId)
            .IsUnique();

        modelBuilder?.Entity<UserServerAccess>()
            .HasIndex(a => new { a.UserId, a.ServerId })
            .IsUnique();

        modelBuilder?.Entity<UserServerAccess>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder?.Entity<UserServerAccess>()
            .HasOne(a => a.Server)
            .WithMany(s => s.UserAccesses)
            .HasForeignKey(a => a.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder?.Entity<AccountDeletion>(entity =>
        {
            entity.HasIndex(a => new { a.UserId, a.ConfirmationCode, a.IsUsed });
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken indexes for performance optimization
        modelBuilder?.Entity<RefreshToken>(entity =>
        {
            // Index for token validation (GetByTokenAsync)
            entity.HasIndex(r => new { r.Token, r.IsRevoked, r.ExpiresAt });
            
            // Index for user's active tokens (GetByUserIdAsync, RevokeAllUserTokensAsync)
            entity.HasIndex(r => new { r.UserId, r.IsRevoked, r.ExpiresAt });
            
            // Additional indexes for common queries
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.ExpiresAt);
            entity.HasIndex(r => r.IsRevoked);
            
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserExternalLogin configuration
        modelBuilder?.Entity<UserExternalLogin>(entity =>
        {
            // Unique index for provider + provider user id
            entity.HasIndex(el => new { el.Provider, el.ProviderUserId })
                .IsUnique();
            
            // Index for user's external logins
            entity.HasIndex(el => el.UserId);
            
            // Index for provider-specific queries
            entity.HasIndex(el => el.Provider);
            
            entity.Property(el => el.Provider)
                .HasConversion<int>();
            
            entity.HasOne(el => el.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(el => el.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Billing configuration
        modelBuilder?.Entity<SubscriptionPlanEntity>(entity =>
        {
            entity.HasIndex(sp => sp.PlanType).IsUnique();
            entity.Property(sp => sp.PlanType).HasConversion<int>();
            entity.Property(sp => sp.Features)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
        });

        modelBuilder?.Entity<Subscription>(entity =>
        {
            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => new { s.Status, s.CurrentPeriodEnd });
            entity.HasIndex(s => s.ProviderSubscriptionId);
            entity.Property(s => s.PlanType).HasConversion<int>();
            entity.Property(s => s.Status).HasConversion<int>();
            entity.Property(s => s.Provider).HasConversion<int>();
        });

        modelBuilder?.Entity<Payment>(entity =>
        {
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => p.SubscriptionId);
            entity.HasIndex(p => p.ProviderPaymentId);
            entity.HasIndex(p => new { p.Status, p.CreatedAt });
            entity.Property(p => p.Status).HasConversion<int>();
            entity.Property(p => p.Provider).HasConversion<int>();
            entity.Property(p => p.Metadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
        });

        modelBuilder?.Entity<WebhookEvent>(entity =>
        {
            entity.HasIndex(w => w.EventId).IsUnique();
            entity.HasIndex(w => new { w.IsProcessed, w.ProcessingAttempts });
            entity.Property(w => w.Provider).HasConversion<int>();
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Remove dangerous SQL logging to console
        // SQL queries should be logged through proper logging infrastructure only in development
        // optionsBuilder?.LogTo(Console.WriteLine); // REMOVED FOR SECURITY
    }
}
