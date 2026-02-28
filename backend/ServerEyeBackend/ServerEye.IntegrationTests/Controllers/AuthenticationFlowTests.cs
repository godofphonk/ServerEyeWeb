namespace ServerEye.IntegrationTests.Controllers;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using ServerEye.Core.DTOs.UserDto;
using System.Text.Json;

[Collection("AuthenticationFlow Tests")]
public class AuthenticationFlowTests : IClassFixture<TestApplicationFactory>, IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public AuthenticationFlowTests(TestApplicationFactory factory)
    {
        this.factory = factory;
        this.client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await this.factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CompleteAuthFlow_RegisterLoginAndAccessProtectedEndpoint_ShouldWork()
    {
        var uniqueEmail = $"authflow_{Guid.NewGuid():N}@example.com";
        
        var registerDto = new UserRegisterDto
        {
            UserName = $"authuser_{Guid.NewGuid():N}",
            Email = uniqueEmail,
            Password = "Test123!"
        };

        var registerResponse = await this.client.PostAsJsonAsync("/api/users/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginDto = new UserLoginDto
        {
            Email = uniqueEmail,
            Password = "Test123!"
        };

        var loginResponse = await this.client.PostAsJsonAsync("/api/users/login", loginDto);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
        var token = loginResult.GetProperty("token").GetString();

        token.Should().NotBeNullOrEmpty();

        this.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var protectedResponse = await this.client.GetAsync("/api/users");

        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";
        
        var registerDto1 = new UserRegisterDto
        {
            UserName = $"user1_{Guid.NewGuid():N}",
            Email = email,
            Password = "Test123!"
        };

        await this.client.PostAsJsonAsync("/api/users/register", registerDto1);

        var registerDto2 = new UserRegisterDto
        {
            UserName = $"user2_{Guid.NewGuid():N}",
            Email = email,
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto2);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_MultipleAttempts_ShouldAllSucceed()
    {
        var email = $"multilogin_{Guid.NewGuid():N}@example.com";
        
        var registerDto = new UserRegisterDto
        {
            UserName = $"multiuser_{Guid.NewGuid():N}",
            Email = email,
            Password = "Test123!"
        };

        await this.client.PostAsJsonAsync("/api/users/register", registerDto);

        var loginDto = new UserLoginDto
        {
            Email = email,
            Password = "Test123!"
        };

        for (int i = 0; i < 3; i++)
        {
            var response = await this.client.PostAsJsonAsync("/api/users/login", loginDto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        var response = await this.client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithInvalidToken_ShouldReturn401()
    {
        this.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        
        var response = await this.client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ShouldReturnTokenInResponse()
    {
        var registerDto = new UserRegisterDto
        {
            UserName = $"tokenuser_{Guid.NewGuid():N}",
            Email = $"token_{Guid.NewGuid():N}@example.com",
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("token");
        
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var token = result.GetProperty("token").GetString();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldReturnUserInformation()
    {
        var email = $"userinfo_{Guid.NewGuid():N}@example.com";
        var userName = $"infouser_{Guid.NewGuid():N}";
        
        var registerDto = new UserRegisterDto
        {
            UserName = userName,
            Email = email,
            Password = "Test123!"
        };

        await this.client.PostAsJsonAsync("/api/users/register", registerDto);

        var loginDto = new UserLoginDto
        {
            Email = email,
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/login", loginDto);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("user");
        
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var user = result.GetProperty("user");
        user.GetProperty("email").GetString().Should().Be(email);
    }
}
