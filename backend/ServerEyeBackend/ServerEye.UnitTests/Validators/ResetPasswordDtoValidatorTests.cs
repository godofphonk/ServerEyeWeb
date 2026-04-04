namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Auth;

public class ResetPasswordDtoValidatorTests
{
    private readonly ResetPasswordDtoValidator sut;

    public ResetPasswordDtoValidatorTests()
    {
        this.sut = new ResetPasswordDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var dto = new ResetPasswordDto
        {
            Token = "some-valid-reset-token",
            NewPassword = "NewPass123"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyToken_ShouldFail(string token)
    {
        var dto = new ResetPasswordDto
        {
            Token = token!,
            NewPassword = "NewPass123"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyNewPassword_ShouldFail(string password)
    {
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            NewPassword = password!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_WithShortNewPassword_ShouldFail()
    {
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            NewPassword = "Sh0rt"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && e.ErrorMessage.Contains('8'));
    }

    [Fact]
    public void Validate_WithPasswordMissingUppercase_ShouldFail()
    {
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            NewPassword = "nouppercase1"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("uppercase"));
    }

    [Fact]
    public void Validate_WithPasswordMissingLowercase_ShouldFail()
    {
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            NewPassword = "NOLOWERCASE1"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("lowercase"));
    }

    [Fact]
    public void Validate_WithPasswordMissingNumber_ShouldFail()
    {
        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            NewPassword = "NoNumbersHere"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("number"));
    }
}
