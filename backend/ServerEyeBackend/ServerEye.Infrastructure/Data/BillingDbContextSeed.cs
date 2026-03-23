namespace ServerEye.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;

public static class BillingDbContextSeed
{
    public static async Task SeedAsync()
    {
        // Seed disabled - entity structure changed
        await Task.CompletedTask;
    }
}
