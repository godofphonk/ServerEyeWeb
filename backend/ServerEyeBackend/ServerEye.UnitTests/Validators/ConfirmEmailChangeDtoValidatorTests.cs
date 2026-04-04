namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Auth;

internal class ConfirmEmailChangeDtoValidatorTests
{
    private readonly ConfirmEmailChangeDtoValidator sut;

    public ConfirmEmailChangeDtoValidatorTests()
    {
        this.sut = new ConfirmEmailChangeDtoValidator();
    }

    [Fact]
    public void Validate_WithValid6DigitCode_ShouldPass()
    {
        var dto = new ConfirmEmailChangeDto
        {
            Code = "654321"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyCode_ShouldFail(string code)
    {
        var dto = new ConfirmEmailChangeDto
        {
            Code = code!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    public void Validate_WithWrongLengthCode_ShouldFail(string code)
    {
        var dto = new ConfirmEmailChangeDto
        {
            Code = code
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("abcdef")]
    [InlineData("!23456")]
    public void Validate_WithNonDigitCode_ShouldFail(string code)
    {
        var dto = new ConfirmEmailChangeDto
        {
            Code = code
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }
}
