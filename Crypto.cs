// AI Assistant Acknowledgement: This file was created or modified with assistance from an AI programming assistant named "GitHub Copilot".
// Review generated code before use and treat any embedded secrets appropriately.
using System;
using System.Security.Cryptography;
using System.Text;

public static class Crypto
{
    // Derive a 32-byte Monthly Master Key from RootKey + DateCode using SHA256
    public static byte[] DeriveMonthlyMasterKey(string rootKey, string dateCode)
    {
        if (rootKey == null) throw new ArgumentNullException(nameof(rootKey));
        if (dateCode == null) throw new ArgumentNullException(nameof(dateCode));

        string combined = rootKey + dateCode;
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
    }

    // Compute HMAC-SHA256 of data using key
    public static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    // Convert hash bytes to a deterministic password string of given length.
    // Uses a URL-safe base62-like alphabet: [A-Za-z0-9_-] mapped to required length.
    public static string HashToPassword(byte[] hash, int length = 16)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";
        var sb = new StringBuilder(length);
        // Use repeated hashing bytes if needed
        int alphaLen = alphabet.Length;
        for (int i = 0; i < length; i++)
        {
            // Combine two bytes to produce a wider distribution
            int idx = (hash[i % hash.Length] + (hash[(i * 7) % hash.Length] << 8)) & 0xFFFF;
            sb.Append(alphabet[idx % alphaLen]);
        }

        return sb.ToString();
    }
}
