namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Server;

public class AddServerRequestValidatorTests
{
    private readonly AddServerRequestValidator sut;

    public AddServerRequestValidatorTests()
    {
        this.sut = new AddServerRequestValidator();
    }

    [Fact]
    public void Validate_WithValidServerKey_ShouldPass()
    {
        var dto = new AddServerRequest
        {
            ServerKey = "valid-server-key-12345"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithExactly10CharKey_ShouldPass()
    {
        var dto = new AddServerRequest
        {
            ServerKey = "abcdefghij"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyServerKey_ShouldFail(string key)
    {
        var dto = new AddServerRequest
        {
            ServerKey = key!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServerKey");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("123456789")]
    [InlineData("a")]
    public void Validate_WithTooShortServerKey_ShouldFail(string key)
    {
        var dto = new AddServerRequest
        {
            ServerKey = key
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServerKey" && e.ErrorMessage.Contains("10"));
    }
}
