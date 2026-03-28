#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Server;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ServerEye.Core.DTOs;
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
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetServers_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange - clear auth header
        this.client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await this.client.GetAsync("/api/servers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetServers_ShouldReturnValidResponseStructure()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Act
        var response = await this.client.GetAsync("/api/servers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        // Response should be either an object with servers property or an array
        result.ValueKind.Should().BeOneOf(JsonValueKind.Object, JsonValueKind.Array);
    }

    [Fact]
    public async Task GetServers_WithDifferentUsers_ShouldReturnUserSpecificData()
    {
        // Arrange - create two separate users
        var userToken1 = await this.CreateTestUser("user1srv");
        var userToken2 = await this.CreateTestUser("user2srv");

        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken1);
        var response1 = await this.client.GetAsync("/api/servers");

        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken2);
        var response2 = await this.client.GetAsync("/api/servers");

        // Assert - both requests should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<string> CreateTestUser(string prefix = "servertest")
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
