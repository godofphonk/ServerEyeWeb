#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Services.Encryption;

using System;
using FluentAssertions;
using ServerEye.Core.Configuration;
using ServerEye.Core.Services;
using Xunit;

public class EncryptionServiceTests
{
    private readonly EncryptionService encryptionService;
    private const string ValidKey = "ThisIsASecureEncryptionKeyWith32Chars!";

    public EncryptionServiceTests()
    {
        var settings = new EncryptionSettings
        {
            Key = ValidKey
        };
        this.encryptionService = new EncryptionService(settings);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidKey_ShouldCreateService()
    {
        // Arrange
        var settings = new EncryptionSettings { Key = ValidKey };

        // Act
        var service = new EncryptionService(settings);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EncryptionService(null!));
    }

    [Fact]
    public void Constructor_WithShortKey_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new EncryptionSettings { Key = "ShortKey" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new EncryptionService(settings));
        exception.Message.Should().Contain("at least 32 characters");
    }

    [Fact]
    public void Constructor_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new EncryptionSettings { Key = string.Empty };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EncryptionService(settings));
    }

    [Fact]
    public void Constructor_WithWhitespaceKey_ShouldThrowArgumentException()
    {
        // Arrange
        var settings = new EncryptionSettings { Key = "                                    " };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EncryptionService(settings));
    }

    #endregion

    #region Encrypt Tests

    [Fact]
    public void Encrypt_WithValidPlainText_ShouldReturnBase64String()
    {
        // Arrange
        const string plainText = "Hello, World!";

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);

        // Assert
        encrypted.Should().NotBeNullOrWhiteSpace();
        encrypted.Should().NotBe(plainText);

        // Verify it's valid Base64
        Action act = () => Convert.FromBase64String(encrypted);
        act.Should().NotThrow();
    }

    [Fact]
    public void Encrypt_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => this.encryptionService.Encrypt(string.Empty));
    }

    [Fact]
    public void Encrypt_WithWhitespace_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => this.encryptionService.Encrypt("   "));
    }

    [Fact]
    public void Encrypt_SamePlainTextTwice_ShouldProduceDifferentCipherTexts()
    {
        // Arrange
        const string plainText = "Test Message";

        // Act
        var encrypted1 = this.encryptionService.Encrypt(plainText);
        var encrypted2 = this.encryptionService.Encrypt(plainText);

        // Assert
        encrypted1.Should().NotBe(encrypted2, "because each encryption should use a unique IV");
    }

    [Fact]
    public void Encrypt_WithSpecialCharacters_ShouldEncryptSuccessfully()
    {
        // Arrange
        const string plainText = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);

        // Assert
        encrypted.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Encrypt_WithUnicodeCharacters_ShouldEncryptSuccessfully()
    {
        // Arrange
        const string plainText = "Unicode: 你好世界 مرحبا العالم Привет мир";

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);

        // Assert
        encrypted.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Encrypt_WithLongText_ShouldEncryptSuccessfully()
    {
        // Arrange
        var plainText = new string('A', 10000);

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);

        // Assert
        encrypted.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Decrypt Tests

    [Fact]
    public void Decrypt_WithValidCipherText_ShouldReturnOriginalPlainText()
    {
        // Arrange
        const string plainText = "Secret Message";
        var encrypted = this.encryptionService.Encrypt(plainText);

        // Act
        var decrypted = this.encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => this.encryptionService.Decrypt(string.Empty));
    }

    [Fact]
    public void Decrypt_WithWhitespace_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => this.encryptionService.Decrypt("   "));
    }

    [Fact]
    public void Decrypt_WithInvalidBase64_ShouldThrowFormatException()
    {
        // Arrange
        const string invalidBase64 = "This is not valid base64!@#$";

        // Act & Assert
        Assert.Throws<FormatException>(() => this.encryptionService.Decrypt(invalidBase64));
    }

    [Fact]
    public void Decrypt_WithTamperedCipherText_ShouldThrowCryptographicException()
    {
        // Arrange
        const string plainText = "Original Message";
        var encrypted = this.encryptionService.Encrypt(plainText);

        // Tamper with the encrypted data
        var tamperedBytes = Convert.FromBase64String(encrypted);
        tamperedBytes[tamperedBytes.Length - 1] ^= 0xFF; // Flip bits in last byte
        var tamperedEncrypted = Convert.ToBase64String(tamperedBytes);

        // Act & Assert
        Assert.Throws<System.Security.Cryptography.CryptographicException>(
            () => this.encryptionService.Decrypt(tamperedEncrypted));
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData("Simple text")]
    [InlineData("Text with numbers 12345")]
    [InlineData("Special chars: !@#$%^&*()")]
    [InlineData("Unicode: 你好 مرحبا Привет")]
    [InlineData("Long text with multiple words and sentences. This should work fine.")]
    public void EncryptDecrypt_RoundTrip_ShouldReturnOriginalText(string plainText)
    {
        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);
        var decrypted = this.encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_WithMultilineText_ShouldPreserveFormatting()
    {
        // Arrange
        const string plainText = "Line 1\nLine 2\r\nLine 3\tTabbed";

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);
        var decrypted = this.encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_WithJsonData_ShouldPreserveStructure()
    {
        // Arrange
        const string jsonData = "{\"name\":\"John\",\"age\":30,\"city\":\"New York\"}";

        // Act
        var encrypted = this.encryptionService.Encrypt(jsonData);
        var decrypted = this.encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(jsonData);
    }

    #endregion

    #region Security Tests

    [Fact]
    public void Encrypt_WithDifferentKeys_ShouldProduceIncompatibleCipherTexts()
    {
        // Arrange
        const string plainText = "Secret Data";
        var settings1 = new EncryptionSettings { Key = "FirstKeyWith32CharactersMinimum!" };
        var settings2 = new EncryptionSettings { Key = "SecondKeyWith32CharactersMinimum" };

        var service1 = new EncryptionService(settings1);
        var service2 = new EncryptionService(settings2);

        // Act
        var encrypted = service1.Encrypt(plainText);

        // Assert - service2 should not be able to decrypt data encrypted by service1
        Assert.Throws<System.Security.Cryptography.CryptographicException>(
            () => service2.Decrypt(encrypted));
    }

    [Fact]
    public void Encrypt_ShouldIncludeIVInCipherText()
    {
        // Arrange
        const string plainText = "Test";

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);
        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Assert
        // AES IV is 16 bytes, encrypted data should be longer
        encryptedBytes.Length.Should().BeGreaterThan(16);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Encrypt_WithSingleCharacter_ShouldWork()
    {
        // Arrange
        const string plainText = "A";

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);
        var decrypted = this.encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_WithNumericString_ShouldWork()
    {
        // Arrange
        const string plainText = "1234567890";

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);
        var decrypted = this.encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_WithEmailAddress_ShouldWork()
    {
        // Arrange
        const string plainText = "user@example.com";

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);
        var decrypted = this.encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_WithUrl_ShouldWork()
    {
        // Arrange
        const string plainText = "https://example.com/path?query=value&other=123";

        // Act
        var encrypted = this.encryptionService.Encrypt(plainText);
        var decrypted = this.encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    #endregion
}
