namespace ServerEye.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class TicketDbContextFactory : IDesignTimeDbContextFactory<TicketDbContext>
{
    public TicketDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TicketDbContext>();
        
        // Use default connection string for migrations
        var connectionString = "Host=localhost;Port=5434;Database=ServerEyeWeb_Dev_Ticket;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString);

        return new TicketDbContext(optionsBuilder.Options);
    }
}
