namespace ServerEye.IntegrationTests;

using Microsoft.Extensions.DependencyInjection;
using ServerEye.Infrastructure;

public class IntegrationTestBase : IClassFixture<TestApplicationFactory>
{
    protected readonly TestApplicationFactory Factory;
    protected readonly HttpClient Client;

    public IntegrationTestBase(TestApplicationFactory factory)
    {
        this.Factory = factory;
        this.Client = factory.CreateClient();
        
        // Clean database before each test
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerEyeDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}
