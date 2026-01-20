namespace ServerEye.Infrastracture;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;

public sealed class ServerEyeDbContext : DbContext
{
    public ServerEyeDbContext(DbContextOptions<ServerEyeDbContext> options)
        : base(options) => this.Database.EnsureCreated();
    public DbSet<Role> Roles => this.Set<Role>();
    public DbSet<User> Users => this.Set<User>();
    public DbSet<ServerEntity> Servers => this.Set<ServerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder?.Entity<ServerEntity>()
            .HasIndex(s => s.ServerKey)
            .IsUnique();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder?.LogTo(Console.WriteLine);
}
