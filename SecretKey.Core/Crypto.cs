using System;
using System.Security.Cryptography;
using System.Text;

namespace SecretKey.Core;

public static class Crypto
{
    public static byte[] DeriveMonthlyMasterKey(string rootKey, string dateCode)
    {
        if (rootKey == null) throw new ArgumentNullException(nameof(rootKey));
        if (dateCode == null) throw new ArgumentNullException(nameof(dateCode));

        string combined = rootKey + dateCode;
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
    }

    public static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    public static string MapBlobToPattern(byte[] hash, string mask)
    {
        if (hash == null) throw new ArgumentNullException(nameof(hash));
        if (mask == null) throw new ArgumentNullException(nameof(mask));

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
                    result[i] = m;
                    break;
            }
            byteIndex++;
        }

        return new string(result);
    }
}
