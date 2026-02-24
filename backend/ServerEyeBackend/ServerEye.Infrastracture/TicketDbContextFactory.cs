namespace ServerEye.Infrastracture;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class TicketDbContextFactory : IDesignTimeDbContextFactory<TicketDbContext>
{
    public TicketDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<TicketDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString(nameof(TicketDbContext)));

        return new TicketDbContext(optionsBuilder.Options);
    }
}
