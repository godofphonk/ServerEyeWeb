namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Auth;

public class ResendVerificationDtoValidatorTests
{
    private readonly ResendVerificationDtoValidator sut;

    public ResendVerificationDtoValidatorTests()
    {
        this.sut = new ResendVerificationDtoValidator();
    }

    [Fact]
    public void Validate_WithValidEmail_ShouldPass()
    {
        var dto = new ResendVerificationDto
        {
            Email = "test@example.com"
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
        var dto = new ResendVerificationDto
        {
            Email = email!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test")]
    public void Validate_WithInvalidEmail_ShouldFail(string email)
    {
        var dto = new ResendVerificationDto
        {
            Email = email
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
