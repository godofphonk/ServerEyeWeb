namespace ServerEye.Infrastracture;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;

public sealed class ServerEyeDbContext : DbContext
{
    public ServerEyeDbContext(DbContextOptions<ServerEyeDbContext> options)
        : base(options) => this.Database.EnsureCreated();
    public DbSet<User> Users => this.Set<User>();
    public DbSet<ServerEntity> Servers => this.Set<ServerEntity>();
    public DbSet<RefreshToken> RefreshTokens => this.Set<RefreshToken>();
    public DbSet<Server> MonitoredServers => this.Set<Server>();
    public DbSet<UserServerAccess> UserServerAccesses => this.Set<UserServerAccess>();
    public DbSet<EmailVerification> EmailVerifications => this.Set<EmailVerification>();
    public DbSet<PasswordResetToken> PasswordResetTokens => this.Set<PasswordResetToken>();
    public DbSet<AccountDeletion> AccountDeletions => this.Set<AccountDeletion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder?.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
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
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder?.LogTo(Console.WriteLine);
}
