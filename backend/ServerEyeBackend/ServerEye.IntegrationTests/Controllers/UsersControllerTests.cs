namespace ServerEye.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ServerEye.Core.DTOs.UserDto;

[Collection("UsersController Tests")]
public class UsersControllerTests : IClassFixture<TestApplicationFactory>, IAsyncLifetime
{
    private readonly TestApplicationFactory factory;

    public UsersControllerTests(TestApplicationFactory factory)
    {
        this.factory = factory;
        // Client will be created in each test method after JWT is configured
    }

    public async Task InitializeAsync()
    {
        await this.factory.EnsureDatabaseCreatedAsync();
        await this.factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_WithValidData_ShouldReturnOk()
    {
        var registerDto = new UserRegisterDto
        {
            UserName = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "Test123!"
        };

        using var client = this.factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/users/register", registerDto);
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Expected 200 OK but got {response.StatusCode}. Response: {content}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("token");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        var registerDto = new UserRegisterDto
        {
            UserName = "testuser",
            Email = "invalid-email",
            Password = "Test123!"
        };

        using var client = this.factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/users/register", registerDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ShouldReturnBadRequest()
    {
        var registerDto = new UserRegisterDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = "123"
        };

        using var client = this.factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/users/register", registerDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOk()
    {
        var uniqueEmail = $"login_{Guid.NewGuid():N}@example.com";
        var registerDto = new UserRegisterDto
        {
            UserName = $"loginuser_{Guid.NewGuid():N}",
            Email = uniqueEmail,
            Password = "Test123!"
        };

        using var client = this.factory.CreateClient();
        await client.PostAsJsonAsync("/api/users/register", registerDto);

        var loginDto = new UserLoginDto
        {
            Email = uniqueEmail,
            Password = "Test123!"
        };

        var response = await client.PostAsJsonAsync("/api/users/login", loginDto);
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Login failed with status {response.StatusCode}. Response: {content}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("token");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        var uniqueEmail = $"wrongpass_{Guid.NewGuid():N}@example.com";
        var registerDto = new UserRegisterDto
        {
            UserName = $"wrongpassuser_{Guid.NewGuid():N}",
            Email = uniqueEmail,
            Password = "Test123!"
        };

        using var client = this.factory.CreateClient();
        await client.PostAsJsonAsync("/api/users/register", registerDto);

        var loginDto = new UserLoginDto
        {
            Email = uniqueEmail,
            Password = "WrongPassword123!"
        };

        var response = await client.PostAsJsonAsync("/api/users/login", loginDto);
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.NotFound && response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new Exception($"Expected NotFound or Unauthorized but got {response.StatusCode}. Response: {content}");
        }

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_ShouldReturnHealthy()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task HealthLive_ShouldReturnHealthy()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthReady_ShouldReturnHealthy()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().Should().Be("Healthy");
    }
}
