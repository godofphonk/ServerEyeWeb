namespace ServerEye.UnitTests.Services;

using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastracture.ExternalServices;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class GoApiClientTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> mockHandler;
    private readonly HttpClient httpClient;
    private readonly GoApiClient goApiClient;

    public GoApiClientTests()
    {
        this.mockHandler = new Mock<HttpMessageHandler>();
        var mockLogger1 = new Mock<ILogger<GoApiClient>>();
        this.httpClient = new HttpClient(this.mockHandler.Object);
        this.goApiClient = new GoApiClient(this.httpClient, mockLogger1.Object);
    }

    public void Dispose()
    {
        this.httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AddServerSourceAsync_ShouldReturnResponse_WhenApiCallSucceeds()
    {
        // Arrange
        var serverId = "srv_123";
        var source = "Web";
        var expectedResponse = new GoApiSourceResponse
        {
            ServerId = serverId,
            Source = source,
            Message = "Source added successfully"
        };

        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        responseMessage.Content = new StringContent(JsonSerializer.Serialize(expectedResponse), Encoding.UTF8, "application/json");

        this.mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await this.goApiClient.AddServerSourceAsync(serverId, source);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.ServerId);
        Assert.Equal(serverId, result.ServerId);
        Assert.Equal(source, result.Source);
        Assert.Equal("Source added successfully", result.Message);

        this.mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains($"/api/servers/{serverId}/sources")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AddServerSourceByKeyAsync_ShouldReturnResponse_WhenApiCallSucceeds()
    {
        // Arrange
        const string serverKey = "test_server_key";
        const string source = "Web";
        var expectedResponse = new GoApiSourceResponse
        {
            ServerId = "srv_123",
            Source = source,
            Message = "Source added successfully"
        };

        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        responseMessage.Content = new StringContent(JsonSerializer.Serialize(expectedResponse), Encoding.UTF8, "application/json");

        this.mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await this.goApiClient.AddServerSourceByKeyAsync(serverKey, source);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.ServerId);
        Assert.Equal("srv_123", result.ServerId);
        Assert.Equal(source, result.Source);
        Assert.Equal("Source added successfully", result.Message);

        this.mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains($"/api/servers/by-key/{serverKey}/sources")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AddServerSourceIdentifiersAsync_ShouldReturnResponse_WhenApiCallSucceeds()
    {
        // Arrange
        var serverId = "srv_123";
        var request = new GoApiSourceIdentifiersRequest
        {
            SourceType = "Web",
            Identifiers = new List<string> { "user123" },
            IdentifierType = "user_id",
            Metadata = new Dictionary<string, object> { { "test", "value" } }
        };

        var expectedResponse = new GoApiSourceIdentifiersResponse
        {
            Message = "Identifiers added successfully",
            ServerId = serverId,
            SourceType = "Web",
            Identifiers = ["user123"],
            IdentifierType = "user_id"
        };

        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        responseMessage.Content = new StringContent(JsonSerializer.Serialize(expectedResponse), Encoding.UTF8, "application/json");

        this.mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await this.goApiClient.AddServerSourceIdentifiersAsync(serverId, request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.ServerId);
        Assert.Equal(serverId, result.ServerId);
        Assert.Equal("Web", result.SourceType);
        Assert.Single(result.Identifiers);
        Assert.Equal("user123", result.Identifiers[0]);
        Assert.Equal("user_id", result.IdentifierType);
        Assert.Equal("Identifiers added successfully", result.Message);

        this.mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains($"/api/servers/{serverId}/sources/identifiers")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AddServerSourceIdentifiersByKeyAsync_ShouldReturnResponse_WhenApiCallSucceeds()
    {
        // Arrange
        const string serverKey = "test_server_key";
        var request = new GoApiSourceIdentifiersRequest
        {
            SourceType = "Web",
            Identifiers = ["user123"],
            IdentifierType = "user_id"
        };

        var expectedResponse = new GoApiSourceIdentifiersResponse
        {
            Message = "Identifiers added successfully",
            ServerId = "srv_123",
            SourceType = "Web",
            Identifiers = ["user123"],
            IdentifierType = "user_id"
        };

        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        responseMessage.Content = new StringContent(JsonSerializer.Serialize(expectedResponse), Encoding.UTF8, "application/json");

        this.mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await this.goApiClient.AddServerSourceIdentifiersByKeyAsync(serverKey, request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.ServerId);
        Assert.Equal("srv_123", result.ServerId);
        Assert.Equal("Web", result.SourceType);
        Assert.Single(result.Identifiers);
        Assert.Equal("user123", result.Identifiers[0]);
        Assert.Equal("user_id", result.IdentifierType);

        this.mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains($"/api/servers/by-key/{serverKey}/sources/identifiers")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AddServerSourceAsync_ShouldReturnNull_WhenApiCallFails()
    {
        // Arrange
        const string serverId = "srv_123";
        const string source = "Web";

        using var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
        responseMessage.Content = new StringContent("Bad request", Encoding.UTF8, "application/json");

        this.mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await this.goApiClient.AddServerSourceAsync(serverId, source);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddServerSourceIdentifiersAsync_ShouldReturnNull_WhenApiCallFails()
    {
        // Arrange
        const string serverId = "srv_123";
        var request = new GoApiSourceIdentifiersRequest
        {
            SourceType = "Web",
            Identifiers = ["user123"],
            IdentifierType = "user_id"
        };

        using var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        responseMessage.Content = new StringContent("Internal server error", Encoding.UTF8, "application/json");

        this.mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await this.goApiClient.AddServerSourceIdentifiersAsync(serverId, request);

        // Assert
        Assert.Null(result);
    }
}
