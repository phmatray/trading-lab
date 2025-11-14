// <copyright file="EncryptionService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using TradingBot.Core.Interfaces;

namespace TradingBot.Infrastructure.Configuration;

/// <summary>
/// AES-256 encryption service for securing API keys and secrets.
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionService"/> class.
    /// </summary>
    public EncryptionService()
    {
        // Derive key from machine-specific data
        var password = GetMachineSpecificPassword();
        using var deriveBytes = new Rfc2898DeriveBytes(
            password,
            Encoding.UTF8.GetBytes("TradingBotSalt2025"),
            100000,
            HashAlgorithmName.SHA256);

        _key = deriveBytes.GetBytes(32); // 256 bits
        _iv = deriveBytes.GetBytes(16);  // 128 bits
    }

    /// <inheritdoc/>
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        return Convert.ToBase64String(ciphertextBytes);
    }

    /// <inheritdoc/>
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        var plaintextBytes = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static string GetMachineSpecificPassword()
    {
        // Combine machine-specific data for key derivation
        var machineId = Environment.MachineName;
        var userId = Environment.UserName;
        var osVersion = Environment.OSVersion.VersionString;
        var salt = "TradingBotEncryption2025";

        return $"{machineId}:{userId}:{osVersion}:{salt}";
    }
}
