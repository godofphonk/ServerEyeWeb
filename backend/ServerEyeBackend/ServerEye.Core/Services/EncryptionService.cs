namespace ServerEye.Core.Services;

using System.Security.Cryptography;
using System.Text;
using ServerEye.Core.Configuration;
using ServerEye.Core.Interfaces.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] key;

    public EncryptionService(EncryptionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.Key) || settings.Key.Length < 32)
        {
            throw new ArgumentException("Encryption key must be at least 32 characters long", nameof(settings));
        }

        this.key = SHA256.HashData(Encoding.UTF8.GetBytes(settings.Key));
    }

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        using var aes = Aes.Create();
        aes.Key = this.key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = this.key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[aes.IV.Length];
        var cipherBytes = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
