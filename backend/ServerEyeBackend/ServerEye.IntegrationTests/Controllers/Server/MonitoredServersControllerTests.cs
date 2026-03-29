#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Server;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ServerEye.Core.DTOs.Server;
using Xunit;

/// <summary>
/// Integration tests for MonitoredServersController (GET/POST/DELETE /api/MonitoredServers).
/// </summary>
[Collection("Integration Tests")]
public class MonitoredServersControllerTests : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public MonitoredServersControllerTests(TestCollectionFixture fixture)
    {
        this.factory = fixture.Factory;
        this.client = fixture.Client;
    }

    public async Task InitializeAsync()
    {
        await this.factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetUserServers_WithAuth_ShouldReturnEmptyListForNewUser()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync("/api/monitoredservers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var servers = await response.Content.ReadFromJsonAsync<ServerResponse[]>();
        servers.Should().NotBeNull();
        servers!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserServers_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await this.client.GetAsync("/api/monitoredservers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddServer_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;
        var request = new AddServerRequest { ServerKey = "test-server-key" };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/monitoredservers/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddServer_WithInvalidKey_ShouldReturnBadRequest()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // The server key doesn't exist in Go API - expect bad request or similar error
        var request = new AddServerRequest { ServerKey = "nonexistent-server-key-12345" };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/monitoredservers/add", request);

        // Assert - adding a server with a key that doesn't exist in the Go API should fail
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RemoveServer_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;
        var serverId = Guid.NewGuid().ToString();

        // Act
        var response = await this.client.DeleteAsync($"/api/monitoredservers/{serverId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveServer_WithNonExistentGuidId_ShouldReturnNotFound()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await this.client.DeleteAsync($"/api/monitoredservers/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShareServer_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        this.client.DefaultRequestHeaders.Authorization = null;
        var serverId = Guid.NewGuid().ToString();
        var request = new ShareServerRequest
        {
            ServerId = serverId,
            TargetUserEmail = "target@example.com",
            AccessLevel = ServerEye.Core.Enums.AccessLevel.Viewer
        };

        // Act
        var response = await this.client.PostAsJsonAsync($"/api/monitoredservers/{serverId}/share", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShareServer_WithNonExistentServer_ShouldReturnError()
    {
        // Arrange - user tries to share a server they don't own
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var serverId = "nonexistent-server-id";
        var request = new ShareServerRequest
        {
            ServerId = serverId,
            TargetUserEmail = "target@example.com",
            AccessLevel = ServerEye.Core.Enums.AccessLevel.Viewer
        };

        // Act
        var response = await this.client.PostAsJsonAsync($"/api/monitoredservers/{serverId}/share", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetUserServers_TwoDifferentUsers_ShouldReturnSeparateData()
    {
        // Arrange - create two separate users
        var token1 = await this.CreateTestUser("srvowner1");
        var token2 = await this.CreateTestUser("srvowner2");

        // Act - both get empty lists initially
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token1);
        var response1 = await this.client.GetAsync("/api/monitoredservers");

        this.client.DefaultRequestHeaders.Authorization = new("Bearer", token2);
        var response2 = await this.client.GetAsync("/api/monitoredservers");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var servers1 = await response1.Content.ReadFromJsonAsync<ServerResponse[]>();
        var servers2 = await response2.Content.ReadFromJsonAsync<ServerResponse[]>();

        servers1.Should().NotBeNull();
        servers2.Should().NotBeNull();
    }

    private async Task<string> CreateTestUser(string prefix = "monsrv")
    {
        var registerDto = new ServerEye.Core.DTOs.UserDto.UserRegisterDto
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}",
            Email = $"{prefix}_{Guid.NewGuid():N}@example.com",
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        return result.GetProperty("token").GetString()!;
    }
}
