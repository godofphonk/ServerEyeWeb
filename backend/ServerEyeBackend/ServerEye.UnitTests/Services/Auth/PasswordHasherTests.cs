namespace ServerEye.UnitTests.Services.Auth;

using ServerEye.Core.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher sut;

    public PasswordHasherTests()
    {
        this.sut = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsNonEmptyHash()
    {
        var hash = this.sut.HashPassword("MyPassword123!");

        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsDifferentStringThanInput()
    {
        const string password = "MyPassword123!";

        var hash = this.sut.HashPassword(password);

        hash.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_CalledTwiceWithSamePassword_ReturnsDifferentHashes()
    {
        const string password = "MyPassword123!";

        var hash1 = this.sut.HashPassword(password);
        var hash2 = this.sut.HashPassword(password);

        hash1.Should().NotBe(hash2, "BCrypt uses random salts");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HashPassword_WithEmptyOrNullPassword_ThrowsArgumentException(string password)
    {
        var act = () => this.sut.HashPassword(password);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        const string password = "CorrectHorseBattery1";
        var hash = this.sut.HashPassword(password);

        var result = this.sut.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var hash = this.sut.HashPassword("OriginalPassword1");

        var result = this.sut.VerifyPassword("WrongPassword1", hash);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void VerifyPassword_WithEmptyOrNullPassword_ThrowsArgumentException(string password)
    {
        var act = () => this.sut.VerifyPassword(password, "some-hash");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void VerifyPassword_WithEmptyOrNullHash_ThrowsArgumentException(string hash)
    {
        var act = () => this.sut.VerifyPassword("SomePassword1", hash);

        act.Should().Throw<ArgumentException>();
    }
}
