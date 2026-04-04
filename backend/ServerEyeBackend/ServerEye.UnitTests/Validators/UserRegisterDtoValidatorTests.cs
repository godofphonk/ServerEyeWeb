namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.UserDto;

public class UserRegisterDtoValidatorTests
{
    private readonly UserRegisterDtoValidator sut;

    public UserRegisterDtoValidatorTests()
    {
        this.sut = new UserRegisterDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var dto = new UserRegisterDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
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
        var dto = new UserRegisterDto
        {
            UserName = userName!,
            Email = "test@example.com",
            Password = "Password123!"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserName");
    }

    [Fact]
    public void Validate_WithShortUserName_ShouldFail()
    {
        var dto = new UserRegisterDto
        {
            UserName = "ab",
            Email = "test@example.com",
            Password = "Password123!"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserName" && e.ErrorMessage.Contains("at least 3"));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test")]
    public void Validate_WithInvalidEmail_ShouldFail(string email)
    {
        var dto = new UserRegisterDto
        {
            UserName = "testuser",
            Email = email,
            Password = "Password123!"
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
        var dto = new UserRegisterDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = password!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithShortPassword_ShouldFail()
    {
        var dto = new UserRegisterDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = "Pass1"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("at least 8"));
    }

    [Fact]
    public void Validate_WithAllFieldsEmpty_ShouldFailWithMultipleErrors()
    {
        var dto = new UserRegisterDto
        {
            UserName = string.Empty,
            Email = string.Empty,
            Password = string.Empty
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(2);
        result.Errors.Should().Contain(e => e.PropertyName == "UserName");
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
