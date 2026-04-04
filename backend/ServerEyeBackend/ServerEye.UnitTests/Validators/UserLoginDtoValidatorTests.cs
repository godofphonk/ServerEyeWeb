namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.UserDto;

internal class UserLoginDtoValidatorTests
{
    private readonly UserLoginDtoValidator sut;

    public UserLoginDtoValidatorTests()
    {
        this.sut = new UserLoginDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var dto = new UserLoginDto
        {
            Email = "user@example.com",
            Password = "SomePassword123"
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
        var dto = new UserLoginDto
        {
            Email = email!,
            Password = "SomePassword123"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@missing.com")]
    [InlineData("missing@")]
    [InlineData("no-at-sign")]
    public void Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        var dto = new UserLoginDto
        {
            Email = email,
            Password = "SomePassword123"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_ShouldFail(string password)
    {
        var dto = new UserLoginDto
        {
            Email = "user@example.com",
            Password = password!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithBothFieldsEmpty_ShouldFailWithMultipleErrors()
    {
        var dto = new UserLoginDto
        {
            Email = string.Empty,
            Password = string.Empty
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
