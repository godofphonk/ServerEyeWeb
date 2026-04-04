namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Auth;

internal class RequestAccountDeletionDtoValidatorTests
{
    private readonly RequestAccountDeletionDtoValidator sut;

    public RequestAccountDeletionDtoValidatorTests()
    {
        this.sut = new RequestAccountDeletionDtoValidator();
    }

    [Fact]
    public void Validate_WithValidPassword_ShouldPass()
    {
        var dto = new RequestAccountDeletionDto
        {
            Password = "ValidPass1"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_ShouldFail(string password)
    {
        var dto = new RequestAccountDeletionDto
        {
            Password = password!
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithTooShortPassword_ShouldFail()
    {
        var dto = new RequestAccountDeletionDto
        {
            Password = "12345"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains('6'));
    }

    [Fact]
    public void Validate_WithExactly6CharPassword_ShouldPass()
    {
        var dto = new RequestAccountDeletionDto
        {
            Password = "abcdef"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }
}
