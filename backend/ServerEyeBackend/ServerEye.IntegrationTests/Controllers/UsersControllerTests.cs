namespace ServerEye.IntegrationTests.Controllers;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using ServerEye.Core.DTOs.Auth;

public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        this.client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnOk()
    {
        var registerDto = new UserRegisterDto
        {
            UserName = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
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

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);

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

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);

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

        await this.client.PostAsJsonAsync("/api/users/register", registerDto);

        var loginDto = new UserLoginDto
        {
            Email = uniqueEmail,
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
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

        await this.client.PostAsJsonAsync("/api/users/register", registerDto);

        var loginDto = new UserLoginDto
        {
            Email = uniqueEmail,
            Password = "WrongPassword123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await this.client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_ShouldReturnHealthy()
    {
        var response = await this.client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task HealthLive_ShouldReturnHealthy()
    {
        var response = await this.client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthReady_ShouldReturnHealthy()
    {
        var response = await this.client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }
}
