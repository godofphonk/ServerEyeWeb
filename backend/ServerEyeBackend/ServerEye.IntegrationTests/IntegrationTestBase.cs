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
        
        // Clean database before each test using the factory method
        factory.ResetDatabaseAsync().GetAwaiter().GetResult();
    }
}
