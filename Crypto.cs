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

    // Map a 32-byte HMAC blob deterministically to a 12-character password with mask: XxxxxNSxxxNN
    // X = uppercase, x = lowercase, N = number, S = special character
    public static string MapBlobToPattern(byte[] hash)
    {
        if (hash == null) throw new ArgumentNullException(nameof(hash));
        if (hash.Length < 12) throw new ArgumentException("Hash must be at least 12 bytes", nameof(hash));

        // Pools
        char[] upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        char[] lower = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        char[] digits = "0123456789".ToCharArray();
        char[] special = "!@#$%&*()-_=+[]{}<>?".ToCharArray();

        // Explicit mapping for positions 0..11 using sequential bytes from hash
        char c0 = upper[hash[0] % upper.Length];
        char c1 = lower[hash[1] % lower.Length];
        char c2 = lower[hash[2] % lower.Length];
        char c3 = lower[hash[3] % lower.Length];
        char c4 = lower[hash[4] % lower.Length];
        char c5 = digits[hash[5] % digits.Length];
        char c6 = special[hash[6] % special.Length];
        char c7 = lower[hash[7] % lower.Length];
        char c8 = lower[hash[8] % lower.Length];
        char c9 = lower[hash[9] % lower.Length];
        char c10 = digits[hash[10] % digits.Length];
        char c11 = digits[hash[11] % digits.Length];

        return new string(new char[] { c0, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11 });
    }
}
