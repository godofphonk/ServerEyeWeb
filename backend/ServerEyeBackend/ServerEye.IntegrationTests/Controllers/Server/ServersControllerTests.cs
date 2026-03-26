#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Server;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Interfaces.Repository;
using Xunit;

[Collection("Integration Tests")]
public class ServersControllerTests : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public ServersControllerTests(TestCollectionFixture fixture)
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
    public async Task GetServers_WithAuth_ShouldReturnUserServers()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync("/api/servers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var servers = await response.Content.ReadFromJsonAsync<ServerDto[]>();
        servers.Should().NotBeNull();
        servers!.Should().BeEmpty(); // New user should have no servers
    }

    [Fact]
    public async Task GetServers_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await this.client.GetAsync("/api/servers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddServer_WithValidData_ShouldCreateServer()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var serverDto = new CreateServerDto
        {
            Name = "Test Server",
            Hostname = "test.example.com",
            IpAddress = "192.168.1.100",
            Port = 22,
            Description = "Test server for integration testing"
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/servers", serverDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServerDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(serverDto.Name);
        result.Hostname.Should().Be(serverDto.Hostname);
        result.IpAddress.Should().Be(serverDto.IpAddress);
        result.Port.Should().Be(serverDto.Port);
    }

    [Fact]
    public async Task AddServer_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var serverDto = new CreateServerDto
        {
            Name = "Unauthorized Server",
            Hostname = "unauthorized.example.com",
            IpAddress = "192.168.1.200",
            Port = 22
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/servers", serverDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddServer_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var invalidServerDto = new CreateServerDto
        {
            Name = "", // Invalid: empty name
            Hostname = "invalid.example.com",
            IpAddress = "invalid-ip", // Invalid IP format
            Port = 70000 // Invalid port
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/servers", invalidServerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetServerById_WithValidServer_ShouldReturnServer()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a server first
        var createDto = new CreateServerDto
        {
            Name = "Server to Get",
            Hostname = "get.example.com",
            IpAddress = "192.168.1.150",
            Port = 22
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/servers", createDto);
        var createdServer = await createResponse.Content.ReadFromJsonAsync<ServerDto>();

        // Act
        var response = await this.client.GetAsync($"/api/servers/{createdServer!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var server = await response.Content.ReadFromJsonAsync<ServerDto>();
        server.Should().NotBeNull();
        server!.Id.Should().Be(createdServer.Id);
        server.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task GetServerById_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var serverId = Guid.NewGuid();

        // Act
        var response = await this.client.GetAsync($"/api/servers/{serverId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetServerById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);
        var invalidId = Guid.NewGuid();

        // Act
        var response = await this.client.GetAsync($"/api/servers/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateServer_WithValidData_ShouldUpdateServer()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a server first
        var createDto = new CreateServerDto
        {
            Name = "Server to Update",
            Hostname = "update.example.com",
            IpAddress = "192.168.1.160",
            Port = 22
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/servers", createDto);
        var createdServer = await createResponse.Content.ReadFromJsonAsync<ServerDto>();

        var updateDto = new UpdateServerDto
        {
            Name = "Updated Server Name",
            Description = "Updated description"
        };

        // Act
        var response = await this.client.PutAsJsonAsync($"/api/servers/{createdServer!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedServer = await response.Content.ReadFromJsonAsync<ServerDto>();
        updatedServer.Should().NotBeNull();
        updatedServer!.Name.Should().Be(updateDto.Name);
        updatedServer.Description.Should().Be(updateDto.Description);
    }

    [Fact]
    public async Task UpdateServer_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var updateDto = new UpdateServerDto
        {
            Name = "Unauthorized Update"
        };

        // Act
        var response = await this.client.PutAsJsonAsync($"/api/servers/{serverId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteServer_WithValidServer_ShouldDeleteServer()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a server first
        var createDto = new CreateServerDto
        {
            Name = "Server to Delete",
            Hostname = "delete.example.com",
            IpAddress = "192.168.1.170",
            Port = 22
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/servers", createDto);
        var createdServer = await createResponse.Content.ReadFromJsonAsync<ServerDto>();

        // Act
        var response = await this.client.DeleteAsync($"/api/servers/{createdServer!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify server is deleted
        var getResponse = await this.client.GetAsync($"/api/servers/{createdServer.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteServer_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var serverId = Guid.NewGuid();

        // Act
        var response = await this.client.DeleteAsync($"/api/servers/{serverId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetServerMetrics_WithValidServer_ShouldReturnMetrics()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a server first
        var createDto = new CreateServerDto
        {
            Name = "Server with Metrics",
            Hostname = "metrics.example.com",
            IpAddress = "192.168.1.180",
            Port = 22
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/servers", createDto);
        var createdServer = await createResponse.Content.ReadFromJsonAsync<ServerDto>();

        // Act
        var response = await this.client.GetAsync($"/api/servers/{createdServer!.Id}/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<ServerMetricsDto>();
        metrics.Should().NotBeNull();
        metrics!.ServerId.Should().Be(createdServer.Id);
    }

    [Fact]
    public async Task GetServerMetrics_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var serverId = Guid.NewGuid();

        // Act
        var response = await this.client.GetAsync($"/api/servers/{serverId}/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private async Task<string> CreateTestUser()
    {
        var registerDto = new ServerEye.Core.DTOs.UserDto.UserRegisterDto
        {
            UserName = $"servertest_{Guid.NewGuid():N}",
            Email = $"server_{Guid.NewGuid():N}@example.com",
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        return result.GetProperty("token").GetString()!;
    }
}
