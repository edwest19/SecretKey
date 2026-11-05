// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SecretKey.Core;

/// <summary>
/// SecretKey password math, version 2. The exact specification is in docs/password-derivation.md.
///
/// Changing anything in this file changes users' passwords. The known-answer tests in
/// SecretKey.Core.Tests must keep passing; a deliberate change must become a new version.
/// </summary>
public static class Crypto
{
    public const int RootKeyByteLength = 32;

    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string Specials = "!@#$%&*()-_=+[]{}<>?";

    private const string MonthlyKeyLabel = "SecretKey v2 monthly key ";
    private const string PasswordLabel = "SecretKey v2 password";

    /// <summary>
    /// Derives the month's key: HKDF-SHA256 over the 32-byte root key, with the date code in the info.
    /// </summary>
    /// <param name="rootKey">The root key as 64 hexadecimal characters (either case; surrounding spaces ignored).</param>
    /// <param name="dateCode">The date code, used exactly as given.</param>
    public static byte[] DeriveMonthlyMasterKey(string rootKey, string dateCode)
    {
        ArgumentNullException.ThrowIfNull(rootKey);
        ArgumentNullException.ThrowIfNull(dateCode);

        byte[] root = ParseRootKey(rootKey);
        try
        {
            byte[] info = Encoding.UTF8.GetBytes(MonthlyKeyLabel + dateCode);
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, root, 32, Array.Empty<byte>(), info);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(root);
        }
    }

    /// <summary>
    /// Generates one account's password from the month's key, the account fields and the mask.
    /// Fields are normalized first (see <see cref="NormalizeField"/> and <see cref="NormalizeUrl"/>).
    /// </summary>
    public static string GeneratePassword(byte[] monthlyKey, string title, string url, string username, string mask)
    {
        ArgumentNullException.ThrowIfNull(monthlyKey);
        ArgumentNullException.ThrowIfNull(mask);

        byte[] accountInput = EncodeFields(PasswordLabel, NormalizeField(title), NormalizeUrl(url), NormalizeField(username), mask);
        return MapStreamToMask(PasswordStream(monthlyKey, accountInput), mask);
    }

    /// <summary>Removes surrounding whitespace and applies Unicode NFC normalization.</summary>
    public static string NormalizeField(string? value) =>
        (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormC);

    /// <summary>Normalizes like any field, then adds "https://" when a non-empty URL has no http:// or https:// prefix.</summary>
    public static string NormalizeUrl(string? url)
    {
        string normalized = NormalizeField(url);
        if (normalized.Length > 0
            && !normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "https://" + normalized;
        }
        return normalized;
    }

    internal static byte[] ParseRootKey(string rootKey)
    {
        string hex = rootKey.Trim();
        if (hex.Length != RootKeyByteLength * 2 || !hex.All(char.IsAsciiHexDigit))
        {
            throw new ArgumentException(
                "The root key must be exactly 64 hexadecimal characters (a 32-byte key). Use \"Generate New Key\" to create one.");
        }
        return Convert.FromHexString(hex);
    }

    /// <summary>Writes each string as a 4-byte big-endian length followed by its UTF-8 bytes.</summary>
    internal static byte[] EncodeFields(params string[] fields)
    {
        var output = new List<byte>();
        Span<byte> length = stackalloc byte[4];
        foreach (string field in fields)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(field);
            BinaryPrimitives.WriteUInt32BigEndian(length, (uint)utf8.Length);
            output.AddRange(length.ToArray());
            output.AddRange(utf8);
        }
        return output.ToArray();
    }

    /// <summary>
    /// An endless deterministic byte stream: HMAC-SHA256(monthlyKey, accountInput || counter) for counter = 0, 1, 2, ...
    /// (counter as a 4-byte big-endian number), concatenated.
    /// </summary>
    internal static IEnumerable<byte> PasswordStream(byte[] monthlyKey, byte[] accountInput)
    {
        using var hmac = new HMACSHA256(monthlyKey);
        byte[] block = new byte[accountInput.Length + 4];
        accountInput.CopyTo(block, 0);

        for (uint counter = 0; ; counter++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(block.AsSpan(accountInput.Length), counter);
            foreach (byte b in hmac.ComputeHash(block))
            {
                yield return b;
            }
        }
    }

    /// <summary>
    /// Builds the password from the mask: X = A-Z, x = a-z, N = 0-9, S = one of "!@#$%&amp;*()-_=+[]{}&lt;&gt;?".
    /// Each token takes the next stream byte that is below the largest multiple of its set size (others are skipped),
    /// so every character in a set is equally likely. Any other mask character is copied as-is and uses no bytes.
    /// </summary>
    internal static string MapStreamToMask(IEnumerable<byte> stream, string mask)
    {
        var password = new StringBuilder(mask.Length);
        using IEnumerator<byte> bytes = stream.GetEnumerator();

        foreach (char token in mask)
        {
            string? set = token switch
            {
                'X' => Upper,
                'x' => Lower,
                'N' => Digits,
                'S' => Specials,
                _ => null,
            };

            if (set is null)
            {
                password.Append(token);
                continue;
            }

            int limit = 256 - (256 % set.Length);
            while (true)
            {
                if (!bytes.MoveNext())
                {
                    throw new InvalidOperationException("The password byte stream ended unexpectedly.");
                }
                if (bytes.Current < limit)
                {
                    password.Append(set[bytes.Current % set.Length]);
                    break;
                }
            }
        }

        return password.ToString();
    }
}
