// <copyright file="EncryptionServiceTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Infrastructure.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="EncryptionService"/>.
/// </summary>
public sealed class EncryptionServiceTests
{
    private readonly EncryptionService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionServiceTests"/> class.
    /// </summary>
    public EncryptionServiceTests()
    {
        _service = new EncryptionService();
    }

    [Fact]
    public void Encrypt_WithValidPlaintext_ReturnsEncryptedString()
    {
        // Arrange
        const string plaintext = "my-api-key-12345";

        // Act
        var encrypted = _service.Encrypt(plaintext);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(plaintext, encrypted);
    }

    [Fact]
    public void Encrypt_WithEmptyString_ReturnsEncryptedString()
    {
        // Arrange
        var plaintext = string.Empty;

        // Act
        var encrypted = _service.Encrypt(plaintext);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
    }

    [Fact]
    public void Encrypt_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.Encrypt(null!));
    }

    [Fact]
    public void Decrypt_WithValidCiphertext_ReturnsOriginalPlaintext()
    {
        // Arrange
        const string plaintext = "my-secret-password";
        var ciphertext = _service.Encrypt(plaintext);

        // Act
        var decrypted = _service.Decrypt(ciphertext);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.Decrypt(null!));
    }

    [Fact]
    public void Decrypt_WithInvalidCiphertext_ThrowsException()
    {
        // Arrange
        const string invalidCiphertext = "invalid-base64-string";

        // Act & Assert
        Assert.Throws<FormatException>(() => _service.Decrypt(invalidCiphertext));
    }

    [Fact]
    public void Decrypt_WithCorruptedCiphertext_ThrowsCryptographicException()
    {
        // Arrange
        var validCiphertext = _service.Encrypt("test");
        var corruptedCiphertext = validCiphertext.Substring(0, validCiphertext.Length - 2) + "AA";

        // Act & Assert
        var exception = Assert.ThrowsAny<Exception>(() => _service.Decrypt(corruptedCiphertext));
        Assert.True(
            exception is System.Security.Cryptography.CryptographicException ||
            exception is FormatException,
            $"Expected CryptographicException or FormatException, but got {exception.GetType().Name}");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("medium-length-api-key-value")]
    [InlineData("very-long-api-key-with-special-characters-!@#$%^&*()_+-=[]{}|;:',.<>?/~`")]
    public void EncryptDecrypt_RoundTrip_PreservesOriginalValue(string plaintext)
    {
        // Act
        var encrypted = _service.Encrypt(plaintext);
        var decrypted = _service.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_SameValueTwice_ProducesSameResult()
    {
        // Arrange
        const string plaintext = "consistent-encryption-test";

        // Act
        var encrypted1 = _service.Encrypt(plaintext);
        var encrypted2 = _service.Encrypt(plaintext);

        // Assert
        Assert.Equal(encrypted1, encrypted2);
    }

    [Fact]
    public void Encrypt_DifferentValues_ProducesDifferentResults()
    {
        // Arrange
        const string plaintext1 = "value1";
        const string plaintext2 = "value2";

        // Act
        var encrypted1 = _service.Encrypt(plaintext1);
        var encrypted2 = _service.Encrypt(plaintext2);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Encrypt_WithSpecialCharacters_WorksCorrectly()
    {
        // Arrange
        const string plaintext = "!@#$%^&*()_+-=[]{}|;:',.<>?/~`\"\\";

        // Act
        var encrypted = _service.Encrypt(plaintext);
        var decrypted = _service.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_WithUnicodeCharacters_WorksCorrectly()
    {
        // Arrange
        const string plaintext = "Hello 世界 🌍 مرحبا мир";

        // Act
        var encrypted = _service.Encrypt(plaintext);
        var decrypted = _service.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_WithWhitespace_PreservesWhitespace()
    {
        // Arrange
        const string plaintext = "  spaces  and\ttabs\nand\rnewlines  ";

        // Act
        var encrypted = _service.Encrypt(plaintext);
        var decrypted = _service.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_WithLongString_WorksCorrectly()
    {
        // Arrange
        var plaintext = new string('A', 10000);

        // Act
        var encrypted = _service.Encrypt(plaintext);
        var decrypted = _service.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void EncryptionService_MultipleInstances_ProduceSameEncryption()
    {
        // Arrange
        const string plaintext = "consistency-test";
        var service1 = new EncryptionService();
        var service2 = new EncryptionService();

        // Act
        var encrypted1 = service1.Encrypt(plaintext);
        var encrypted2 = service2.Encrypt(plaintext);

        // Assert
        Assert.Equal(encrypted1, encrypted2);
    }

    [Fact]
    public void EncryptionService_DifferentInstances_CanDecryptEachOthersData()
    {
        // Arrange
        const string plaintext = "cross-instance-test";
        var service1 = new EncryptionService();
        var service2 = new EncryptionService();

        // Act
        var encrypted = service1.Encrypt(plaintext);
        var decrypted = service2.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_ReturnsBase64String()
    {
        // Arrange
        const string plaintext = "test-base64-encoding";

        // Act
        var encrypted = _service.Encrypt(plaintext);

        // Assert
        Assert.True(IsBase64String(encrypted), "Encrypted string should be valid Base64");
    }

    [Fact]
    public void Encrypt_ProducesNonEmptyResult_ForEmptyInput()
    {
        // Arrange
        var plaintext = string.Empty;

        // Act
        var encrypted = _service.Encrypt(plaintext);

        // Assert
        Assert.NotEmpty(encrypted);
    }

    [Fact]
    public void Encrypt_ResultLength_IsReasonable()
    {
        // Arrange
        var plaintext = "test";

        // Act
        var encrypted = _service.Encrypt(plaintext);

        // Assert
        // Base64 encoding + AES padding should produce reasonable length
        Assert.InRange(encrypted.Length, 10, 200);
    }

    private static bool IsBase64String(string base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            return false;
        }

        try
        {
            _ = Convert.FromBase64String(base64);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
