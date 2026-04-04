namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.UserDto;

internal class UserUpdateDtoValidatorTests
{
    private readonly UserUpdateDtoValidator sut;

    public UserUpdateDtoValidatorTests()
    {
        this.sut = new UserUpdateDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var dto = new UserUpdateDto
        {
            UserName = "updateduser",
            Email = "updated@example.com",
            Password = string.Empty
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidDataAndPassword_ShouldPass()
    {
        var dto = new UserUpdateDto
        {
            UserName = "updateduser",
            Email = "updated@example.com",
            Password = "NewPass123"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyUserName_ShouldFail(string userName)
    {
        var dto = new UserUpdateDto
        {
            UserName = userName!,
            Email = "updated@example.com",
            Password = string.Empty
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserName");
    }

    [Fact]
    public void Validate_WithShortUserName_ShouldFail()
    {
        var dto = new UserUpdateDto
        {
            UserName = "ab",
            Email = "updated@example.com",
            Password = string.Empty
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserName");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public void Validate_WithInvalidEmail_ShouldFail(string email)
    {
        var dto = new UserUpdateDto
        {
            UserName = "validuser",
            Email = email,
            Password = string.Empty
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithShortPassword_ShouldFail()
    {
        var dto = new UserUpdateDto
        {
            UserName = "validuser",
            Email = "updated@example.com",
            Password = "12345"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithNullPassword_ShouldSkipPasswordValidation()
    {
        var dto = new UserUpdateDto
        {
            UserName = "validuser",
            Email = "updated@example.com",
            Password = null!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "Password");
    }
}
