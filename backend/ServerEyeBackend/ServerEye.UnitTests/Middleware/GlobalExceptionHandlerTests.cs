namespace ServerEye.UnitTests.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ServerEye.API.Middleware;
using System.IO;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> loggerMock;
    private readonly GlobalExceptionHandler sut;

    public GlobalExceptionHandlerTests()
    {
        this.loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        this.sut = new GlobalExceptionHandler(this.loggerMock.Object);
    }

    [Fact]
    public async Task TryHandleAsync_WithArgumentException_ShouldReturn400()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new ArgumentException("Test argument exception");

        var result = await this.sut.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task TryHandleAsync_WithKeyNotFoundException_ShouldReturn404()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new KeyNotFoundException("Resource not found");

        var result = await this.sut.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task TryHandleAsync_WithUnauthorizedAccessException_ShouldReturn401()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new UnauthorizedAccessException("Unauthorized");

        var result = await this.sut.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task TryHandleAsync_WithGenericException_ShouldReturn500()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new Exception("Generic error");

        var result = await this.sut.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldLogException()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new Exception("Test exception");

        await this.sut.TryHandleAsync(context, exception, CancellationToken.None);

        this.loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldIncludeTraceId()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace-id";
        var exception = new Exception("Test exception");

        await this.sut.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        responseBody.Should().Contain("test-trace-id");
    }
}
