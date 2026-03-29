#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - 'Collection' suffix required for xUnit collection definitions
namespace ServerEye.IntegrationTests;

[CollectionDefinition("Integration Tests")]
public class IntegrationTestsCollection : ICollectionFixture<TestCollectionFixture>
{
}
#pragma warning restore CA1711

/// <summary>
/// Shared fixture for the "Integration Tests" collection.
/// Provides a single <see cref="TestApplicationFactory"/> instance and an <see cref="HttpClient"/>
/// that are reused across all tests in the collection.
/// </summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable - disposal handled via IAsyncLifetime.DisposeAsync
public sealed class TestCollectionFixture : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;

    public TestCollectionFixture()
    {
        this.factory = new TestApplicationFactory();
    }

    public TestApplicationFactory Factory => this.factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await ((IAsyncLifetime)this.factory).InitializeAsync();
        await this.factory.EnsureDatabaseCreatedAsync();
        this.Client = this.factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        this.Client?.Dispose();
        await ((IAsyncLifetime)this.factory).DisposeAsync();
    }
}
#pragma warning restore CA1001
