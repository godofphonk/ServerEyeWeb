namespace ServerEye.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class BillingDesignTimeDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BillingDbContext>();

        optionsBuilder.UseNpgsql("Host=127.0.0.1;Port=5436;Database=ServerEyeWeb_Dev_Billing;Username=postgres;Password=postgres");

        return new BillingDbContext(optionsBuilder.Options);
    }
}
