namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Auth;

public class ForgotPasswordDtoValidatorTests
{
    private readonly ForgotPasswordDtoValidator sut;

    public ForgotPasswordDtoValidatorTests()
    {
        this.sut = new ForgotPasswordDtoValidator();
    }

    [Fact]
    public void Validate_WithValidEmail_ShouldPass()
    {
        var dto = new ForgotPasswordDto
        {
            Email = "user@example.com"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyEmail_ShouldFail(string email)
    {
        var dto = new ForgotPasswordDto
        {
            Email = email!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    public void Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        var dto = new ForgotPasswordDto
        {
            Email = email
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithTooLongEmail_ShouldFail()
    {
        var dto = new ForgotPasswordDto
        {
            Email = new string('a', 95) + "@x.com"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("100"));
    }
}
