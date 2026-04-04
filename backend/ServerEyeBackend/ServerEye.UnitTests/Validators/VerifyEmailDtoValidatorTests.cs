namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Auth;

internal class VerifyEmailDtoValidatorTests
{
    private readonly VerifyEmailDtoValidator sut;

    public VerifyEmailDtoValidatorTests()
    {
        this.sut = new VerifyEmailDtoValidator();
    }

    [Fact]
    public void Validate_WithValid6DigitCode_ShouldPass()
    {
        var dto = new VerifyEmailDto
        {
            Code = "000000"
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
        var dto = new VerifyEmailDto
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
    [InlineData("123")]
    public void Validate_WithWrongLengthCode_ShouldFail(string code)
    {
        var dto = new VerifyEmailDto
        {
            Code = code
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("abcdef")]
    [InlineData("12345x")]
    [InlineData("!23456")]
    public void Validate_WithNonDigitCode_ShouldFail(string code)
    {
        var dto = new VerifyEmailDto
        {
            Code = code
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }
}
