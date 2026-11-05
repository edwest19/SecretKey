// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
namespace SecretKey.Core;

/// <summary>
/// Checks settings before any password is made. Each method returns null when the value is
/// valid, or a message for the user explaining what's wrong. The limits are documented in README.md.
/// </summary>
public static class Validation
{
    /// <summary>A mask must contain at least this many random characters (X, x, N or S).</summary>
    public const int MinMaskTokens = 4;

    /// <summary>A mask may be at most this many characters long.</summary>
    public const int MaxMaskLength = 64;

    /// <summary>The mask characters that produce a random character.</summary>
    public const string MaskTokens = "XxNS";

    public static string? RootKeyError(string? rootKey)
    {
        string hex = (rootKey ?? string.Empty).Trim();
        if (hex.Length != Crypto.RootKeyByteLength * 2 || !hex.All(char.IsAsciiHexDigit))
        {
            return "The root key must be exactly 64 hexadecimal characters (a 32-byte key). Use \"Generate New Key\" to create one.";
        }
        return null;
    }

    public static string? DateCodeError(string? dateCode)
    {
        if (dateCode is null || dateCode.Length != 4 || !dateCode.All(char.IsAsciiDigit))
        {
            return "The date code must be 4 digits in YYMM form, for example 2611 for November 2026.";
        }
        int month = (dateCode[2] - '0') * 10 + (dateCode[3] - '0');
        if (month < 1 || month > 12)
        {
            return $"The date code's month must be 01 to 12 (got {dateCode[2..]}).";
        }
        return null;
    }

    public static string? MaskError(string? mask)
    {
        if (string.IsNullOrEmpty(mask))
        {
            return "The password mask is empty.";
        }
        if (mask.Length > MaxMaskLength)
        {
            return $"The password mask is {mask.Length} characters long; the most allowed is {MaxMaskLength}.";
        }
        foreach (char c in mask)
        {
            if (c < '!' || c > '~')
            {
                return $"The password mask contains '{c}' (U+{(int)c:X4}), which isn't allowed. Use X, x, N and S, plus visible ASCII symbols such as - or _ to copy as-is.";
            }
        }
        int tokens = mask.Count(c => MaskTokens.Contains(c));
        if (tokens < MinMaskTokens)
        {
            return $"The password mask needs at least {MinMaskTokens} of X, x, N or S (it has {tokens}).";
        }
        return null;
    }
}
