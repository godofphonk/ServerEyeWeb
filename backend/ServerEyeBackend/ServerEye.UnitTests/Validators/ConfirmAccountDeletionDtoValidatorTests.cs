namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Auth;

internal class ConfirmAccountDeletionDtoValidatorTests
{
    private readonly ConfirmAccountDeletionDtoValidator sut;

    public ConfirmAccountDeletionDtoValidatorTests()
    {
        this.sut = new ConfirmAccountDeletionDtoValidator();
    }

    [Fact]
    public void Validate_WithValid6DigitCode_ShouldPass()
    {
        var dto = new ConfirmAccountDeletionDto
        {
            ConfirmationCode = "123456"
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
        var dto = new ConfirmAccountDeletionDto
        {
            ConfirmationCode = code!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmationCode");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("1")]
    public void Validate_WithWrongLengthCode_ShouldFail(string code)
    {
        var dto = new ConfirmAccountDeletionDto
        {
            ConfirmationCode = code
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmationCode");
    }

    [Theory]
    [InlineData("abcdef")]
    [InlineData("12345a")]
    [InlineData("!@#$%^")]
    public void Validate_WithNonDigitCode_ShouldFail(string code)
    {
        var dto = new ConfirmAccountDeletionDto
        {
            ConfirmationCode = code
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmationCode");
    }
}
