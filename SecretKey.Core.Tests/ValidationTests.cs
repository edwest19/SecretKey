// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
using SecretKey.Core;
using static SecretKey.Core.Tests.TestHelper;

namespace SecretKey.Core.Tests;

/// <summary>Tests for the settings checks (README.md "Masks", "Date code", "Configuration").</summary>
[TestClass]
public sealed class ValidationTests
{
    [TestMethod]
    [DataRow("XxxxxNSxxxNN")]
    [DataRow("NNNN")]                 // a 4-digit PIN: the minimum
    [DataRow("Xxxxxxx-NN")]           // a literal symbol, for sites that allow only some symbols
    [DataRow("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")] // 64 characters: the maximum
    public void ValidMasks_AreAccepted(string mask)
    {
        Assert.IsNull(Validation.MaskError(mask));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("NNN")]          // only 3 random characters
    [DataRow("Password1!")]   // no X, x, N or S at all: every account would get "Password1!"
    [DataRow("Xxxx xxxx")]    // space
    [DataRow("Xxxxé")]        // non-ASCII
    [DataRow("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")] // 65 characters
    public void InvalidMasks_AreRejected(string mask)
    {
        Assert.IsNotNull(Validation.MaskError(mask));
    }

    [TestMethod]
    public void MissingMask_IsRejected()
    {
        Assert.IsNotNull(Validation.MaskError(null));
    }

    [TestMethod]
    [DataRow("2611")]
    [DataRow("0001")]
    [DataRow("9912")]
    public void ValidDateCodes_AreAccepted(string dateCode)
    {
        Assert.IsNull(Validation.DateCodeError(dateCode));
    }

    [TestMethod]
    [DataRow("2600")]   // month 00
    [DataRow("2613")]   // month 13
    [DataRow("261")]
    [DataRow("26111")]
    [DataRow("26-1")]
    [DataRow("abcd")]
    [DataRow("２６１１")] // full-width digits
    [DataRow("")]
    public void InvalidDateCodes_AreRejected(string dateCode)
    {
        Assert.IsNotNull(Validation.DateCodeError(dateCode));
        Assert.ThrowsExactly<ArgumentException>(() => Crypto.DeriveMonthlyMasterKey(TestRootKey, dateCode));
    }

    [TestMethod]
    public void RootKey_IsCheckedTheSameWayEverywhere()
    {
        Assert.IsNull(Validation.RootKeyError(TestRootKey));
        Assert.IsNotNull(Validation.RootKeyError("REPLACE_WITH_YOUR_ROOT_KEY"));
        Assert.IsNotNull(Validation.RootKeyError(null));
    }
}
