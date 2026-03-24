namespace ServerEye.Infrastructure;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;

public sealed class TicketDbContext : DbContext
{
    public TicketDbContext(DbContextOptions<TicketDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => this.Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => this.Set<TicketMessage>();
    public DbSet<TicketAttachment> TicketAttachments => this.Set<TicketAttachment>();
    public DbSet<Notification> Notifications => this.Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder?.Entity<Ticket>()
            .HasIndex(t => t.TicketNumber)
            .IsUnique();

        modelBuilder?.Entity<Ticket>()
            .HasIndex(t => t.Email);

        modelBuilder?.Entity<Ticket>()
            .HasIndex(t => t.Status);

        modelBuilder?.Entity<Ticket>()
            .HasIndex(t => t.CreatedAt);

        modelBuilder?.Entity<TicketMessage>()
            .HasOne(tm => tm.Ticket)
            .WithMany(t => t.Messages)
            .HasForeignKey(tm => tm.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder?.Entity<TicketAttachment>()
            .HasOne(ta => ta.Ticket)
            .WithMany(t => t.Attachments)
            .HasForeignKey(ta => ta.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Remove dangerous SQL logging to console
        // SQL queries should be logged through proper logging infrastructure only in development
        // optionsBuilder?.LogTo(Console.WriteLine); // REMOVED FOR SECURITY
    }
}
