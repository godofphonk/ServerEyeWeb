namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Auth;

internal class ChangeEmailDtoValidatorTests
{
    private readonly ChangeEmailDtoValidator sut;

    public ChangeEmailDtoValidatorTests()
    {
        this.sut = new ChangeEmailDtoValidator();
    }

    [Fact]
    public void Validate_WithValidEmail_ShouldPass()
    {
        var dto = new ChangeEmailDto
        {
            NewEmail = "newemail@example.com"
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
        var dto = new ChangeEmailDto
        {
            NewEmail = email!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewEmail");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@domain.com")]
    [InlineData("missing@")]
    [InlineData("nodot")]
    public void Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        var dto = new ChangeEmailDto
        {
            NewEmail = email
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewEmail");
    }

    [Fact]
    public void Validate_WithTooLongEmail_ShouldFail()
    {
        var dto = new ChangeEmailDto
        {
            NewEmail = new string('a', 92) + "@test.com"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewEmail" && e.ErrorMessage.Contains("100"));
    }
}
