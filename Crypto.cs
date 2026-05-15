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

    // Map a HMAC blob deterministically to a password using a mask string.
    // Mask characters:
    //  X = uppercase, x = lowercase, N = digit, S = special, any other char is used verbatim.
    // Bytes from hash are consumed sequentially (wrapping if needed) and mapped to pools using modulo.
    public static string MapBlobToPattern(byte[] hash, string mask)
    {
        if (hash == null) throw new ArgumentNullException(nameof(hash));
        if (mask == null) throw new ArgumentNullException(nameof(mask));

        // Pools
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
                    // use literal character from mask
                    result[i] = m;
                    break;
            }
            byteIndex++;
        }

        return new string(result);
    }
}
