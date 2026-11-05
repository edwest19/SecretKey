// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SecretKey.Core;
using static SecretKey.Core.Tests.TestHelper;

namespace SecretKey.Core.Tests;

/// <summary>
/// Checks that the facts stated in README.md and docs/password-derivation.md match the code,
/// so the documentation can't silently drift. Behavior claims are proven by the other test
/// classes; CLAUDE.md maps each README section to its code and tests.
/// </summary>
[TestClass]
public sealed class ReadmeClaimsTests
{
    private static string Readme => File.ReadAllText(Path.Combine(RepoRoot(), "README.md"));
    private static string Spec => File.ReadAllText(Path.Combine(RepoRoot(), "docs", "password-derivation.md"));

    [TestMethod]
    public void Readme_MaskSection_MatchesTheCode()
    {
        string readme = Readme;
        Assert.Contains($"`{Crypto.Symbols}`", readme, "README must show the exact symbol set used for S.");
        Assert.Contains($"at most {Validation.MaxMaskLength} characters", readme);
        Assert.Contains($"at least {Validation.MinMaskTokens} of `X`, `x`, `N` or `S`", readme);
        foreach (char token in Validation.MaskTokens)
        {
            Assert.Contains($"| `{token}` |", readme, $"README's mask table must list {token}.");
        }
    }

    [TestMethod]
    public void Readme_DefaultMaskAndExample_MatchTheCode()
    {
        Assert.Contains($"The default is `{SecretKeyConfig.DefaultMask}`", Readme);
        Assert.Contains($"The default mask `{SecretKeyConfig.DefaultMask}` gives passwords like `{Password("Bhphotovideo", "https://www.bhphotovideo.com", "email@sample.com")}`", Readme);
    }

    [TestMethod]
    public void Readme_EveryPasswordMaskExample_IsValid()
    {
        MatchCollection examples = Regex.Matches(Readme, "`passwordmask=([^`]+)`");
        Assert.IsNotEmpty(examples, "README should show at least one passwordmask= example.");
        foreach (Match example in examples)
        {
            (string? mask, string? error) = Processor.FindMaskInNotes(example.Value.Trim('`'));
            Assert.IsNull(error, $"README example {example.Value} is invalid: {error}");
            Assert.IsNotNull(mask);
        }
    }

    [TestMethod]
    public void Readme_InputAndOutputColumns_MatchTheCode()
    {
        string readme = Readme;
        Assert.Contains($"`{string.Join("`, `", Processor.RequiredColumns.SkipLast(1))}` and `{Processor.RequiredColumns[^1]}` are required", readme);
        Assert.Contains($"`{Processor.NotesColumn}` is optional", readme);
        Assert.Contains($"`{Processor.PasswordColumn}` is optional", readme);
        Assert.Contains($"exactly four columns: `{Processor.OutputHeader}`", readme);
    }

    [TestMethod]
    public void ConfigExample_ReadmeTable_AndConfigClass_HaveTheSameSettings()
    {
        string[] classSettings = typeof(SecretKeyConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).OrderBy(n => n).ToArray();

        using JsonDocument example = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "config.example.json")));
        string[] exampleSettings = example.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();

        CollectionAssert.AreEqual(classSettings, exampleSettings, "config.example.json must have exactly the settings SecretKeyConfig reads.");
        foreach (string setting in classSettings)
        {
            Assert.Contains($"| `{setting}` |", Readme, $"README's configuration table must list {setting}.");
        }
        Assert.AreEqual(SecretKeyConfig.DefaultMask, example.RootElement.GetProperty(nameof(SecretKeyConfig.PasswordMask)).GetString());
    }

    [TestMethod]
    public void Readme_LimitsForDateCodeAndRootKey_MatchTheCode()
    {
        string readme = Readme;
        Assert.Contains("Four digits, `YYMM`, with a month from 01 to 12", readme);
        Assert.Contains($"exactly {Crypto.RootKeyByteLength * 2} hexadecimal characters ({Crypto.RootKeyByteLength} bytes)", readme);
    }

    [TestMethod]
    public void Spec_CharacterTable_MatchesTheCode()
    {
        string spec = Spec;
        foreach ((char token, string set) in new[] { ('X', Crypto.UppercaseLetters), ('x', Crypto.LowercaseLetters), ('N', Crypto.Digits), ('S', Crypto.Symbols) })
        {
            int limit = 256 - (256 % set.Length);
            Assert.Contains($"| `{token}` | `{set}` | {set.Length} | {limit} |", spec, $"Spec row for {token} doesn't match the code.");
        }
    }

    [TestMethod]
    public void Spec_Labels_MatchTheCode()
    {
        string spec = Spec;
        Assert.Contains($"UTF-8(\"{Crypto.MonthlyKeyLabel}\" + dateCode)", spec);
        Assert.Contains($"1. `{Crypto.PasswordLabel}`", spec);
    }

    [TestMethod]
    public void GitIgnore_ExcludesTheSecretsTheReadmeSaysItDoes()
    {
        string[] lines = File.ReadAllLines(Path.Combine(RepoRoot(), ".gitignore")).Select(l => l.Trim()).ToArray();
        Assert.Contains("`.gitignore` excludes `config.json` and `*.pfx`", Readme);
        CollectionAssert.Contains(lines, "config.json");
        CollectionAssert.Contains(lines, "*.pfx");
    }
}
