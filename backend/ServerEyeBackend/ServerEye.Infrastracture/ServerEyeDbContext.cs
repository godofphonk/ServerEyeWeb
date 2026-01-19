namespace ServerEye.Infrastracture;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;

public sealed class ServerEyeDbContext : DbContext
{
    public ServerEyeDbContext(DbContextOptions<ServerEyeDbContext> options)
        : base(options) => this.Database.EnsureCreated();

    public DbSet<User> Users => this.Set<User>();
    public DbSet<Role> Roles => this.Set<Role>();
    public DbSet<UserRole> UserRoles => this.Set<UserRole>();
    public DbSet<UserSession> UserSessions => this.Set<UserSession>();
    public DbSet<Server> Servers => this.Set<Server>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder?.Entity<Server>()
            .HasIndex(s => s.ServerKey)
            .IsUnique();
        modelBuilder?.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();
        modelBuilder?.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder?.LogTo(Console.WriteLine);
}
