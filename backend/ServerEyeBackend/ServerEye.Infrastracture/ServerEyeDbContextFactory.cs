namespace ServerEye.Infrastracture;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class ServerEyeDbContextFactory : IDesignTimeDbContextFactory<ServerEyeDbContext>
{
    public ServerEyeDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ServerEyeDbContext>();
        var connectionString = configuration.GetConnectionString("ServerEyeDbContext");

        optionsBuilder.UseNpgsql(connectionString);

        return new ServerEyeDbContext(optionsBuilder.Options);
    }
}
