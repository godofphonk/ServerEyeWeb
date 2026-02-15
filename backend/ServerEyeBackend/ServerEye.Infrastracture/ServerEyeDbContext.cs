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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder?.LogTo(Console.WriteLine);
}
