namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Enums;

public class ShareServerRequestValidatorTests
{
    private readonly ShareServerRequestValidator sut;

    public ShareServerRequestValidatorTests()
    {
        this.sut = new ShareServerRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        var dto = new ShareServerRequest
        {
            ServerId = "server-123",
            TargetUserEmail = "target@example.com",
            AccessLevel = AccessLevel.Viewer
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyServerId_ShouldFail(string serverId)
    {
        var dto = new ShareServerRequest
        {
            ServerId = serverId!,
            TargetUserEmail = "target@example.com",
            AccessLevel = AccessLevel.Viewer
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServerId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyTargetEmail_ShouldFail(string email)
    {
        var dto = new ShareServerRequest
        {
            ServerId = "server-123",
            TargetUserEmail = email!,
            AccessLevel = AccessLevel.Viewer
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetUserEmail");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    public void Validate_WithInvalidTargetEmail_ShouldFail(string email)
    {
        var dto = new ShareServerRequest
        {
            ServerId = "server-123",
            TargetUserEmail = email,
            AccessLevel = AccessLevel.Viewer
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetUserEmail");
    }

    [Theory]
    [InlineData(AccessLevel.None)]
    [InlineData(AccessLevel.Viewer)]
    [InlineData(AccessLevel.Admin)]
    [InlineData(AccessLevel.Owner)]
    public void Validate_WithValidAccessLevels_ShouldPass(AccessLevel level)
    {
        var dto = new ShareServerRequest
        {
            ServerId = "server-123",
            TargetUserEmail = "target@example.com",
            AccessLevel = level
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidAccessLevel_ShouldFail()
    {
        var dto = new ShareServerRequest
        {
            ServerId = "server-123",
            TargetUserEmail = "target@example.com",
            AccessLevel = (AccessLevel)99
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccessLevel");
    }
}
