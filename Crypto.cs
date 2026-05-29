// ============================================================================
// Copyright (c) 2026 edwest19
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// ============================================================================

// AI Assistant Acknowledgement: This file was created or modified with assistance from an AI programming assistant named "GitHub Copilot".
// Review generated code before use and treat any embedded secrets appropriately.

using System;
using System.Security.Cryptography;
using System.Text;

public static class Crypto
{
    /// <summary>
    /// Derive a 32-byte Monthly Master Key from RootKey + DateCode using SHA256.
    /// </summary>
    public static byte[] DeriveMonthlyMasterKey(string rootKey, string dateCode)
    {
        if (rootKey == null) throw new ArgumentNullException(nameof(rootKey));
        if (dateCode == null) throw new ArgumentNullException(nameof(dateCode));

        string combined = rootKey + dateCode;
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
    }

    /// <summary>
    /// Compute HMAC-SHA256 of data using the monthly master key.
    /// </summary>
    public static byte[] HmacSha256(byte[] key, byte[] data)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    /// <summary>
    /// Maps a 32-byte HMAC blob deterministically to a password using an explicit mask layout.
    /// Mask rules:
    ///   X = Uppercase Letter
    ///   x = Lowercase Letter
    ///   N = Numeric Digit
    ///   S = Special Character
    ///   Any other character is left as a literal within the password output.
    /// </summary>
    public static string MapBlobToPattern(byte[] hash, string mask)
    {
        if (hash == null) throw new ArgumentNullException(nameof(hash));
        if (mask == null) throw new ArgumentNullException(nameof(mask));

        // Optimized character lookup spans
        ReadOnlySpan<char> upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".AsSpan();
        ReadOnlySpan<char> lower = "abcdefghijklmnopqrstuvwxyz".AsSpan();
        ReadOnlySpan<char> digits = "0123456789".AsSpan();
        ReadOnlySpan<char> special = "!@#$%&*()-_=+[]{}<>?".AsSpan();

        var result = new char[mask.Length];
        int byteIndex = 0;

        for (int i = 0; i < mask.Length; i++)
        {
            byte b = hash[byteIndex % hash.Length];
            char m = mask[i];

            switch (m)
            {
                case 'X':
                    result[i] = upper[b % upper.Length];
                    break;
                case 'x':
                    result[i] = lower[b % lower.Length];
                    break;
                case 'N':
                    result[i] = digits[b % digits.Length];
                    break;
                case 'S':
                    result[i] = special[b % special.Length];
                    break;
                default:
                    // Treat any other character token as a structural literal
                    result[i] = m;
                    break;
            }
            byteIndex++;
        }

        return new string(result);
    }
}