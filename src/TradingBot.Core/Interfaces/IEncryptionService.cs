// <copyright file="IEncryptionService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Provides encryption and decryption services for sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string using AES-256.
    /// </summary>
    /// <param name="plaintext">The plaintext to encrypt.</param>
    /// <returns>The encrypted ciphertext (Base64 encoded).</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts a ciphertext string using AES-256.
    /// </summary>
    /// <param name="ciphertext">The ciphertext to decrypt (Base64 encoded).</param>
    /// <returns>The decrypted plaintext.</returns>
    string Decrypt(string ciphertext);
}
