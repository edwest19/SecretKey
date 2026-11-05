using static SecretKey.Core.Tests.TestHelper;

namespace SecretKey.Core.Tests;

/// <summary>
/// Locks the passwords SecretKey produces (password math version 2, docs/password-derivation.md).
///
/// Why this exists: the whole point of SecretKey is that the same root key and date code
/// give the same passwords forever. If any test here fails, a code change has altered
/// users' passwords. Do not "fix" the expected values to make a test pass. Change the
/// code back, or put the new behavior behind an explicit new version.
/// </summary>
public class PasswordDerivationTests
{
    [Theory]
    [InlineData("Bhphotovideo", "https://www.bhphotovideo.com", "email@sample.com", DefaultMask, "2611", "Fiipc3]jpv38")]
    [InlineData("Bhphotovideo", "https://www.bhphotovideo.com", "email@sample.com", DefaultMask, "2612", "Fnisb8-erb99")]
    [InlineData("Paramount+", "paramountplus.com", "email@yahoo.com", DefaultMask, "2611", "Xxkch8{gnc91")]
    [InlineData("Paramount+", "paramountplus.com", "email@yahoo.com", "xxxxxxxxNNNN", "2611", "hleziocv3322")]
    [InlineData("MTA eTix", "https://www.mta.info/", "email@sample.com", DefaultMask, "2611", "Qlljp5[oht46")]
    [InlineData("Solterra", "", "email@sample.com", DefaultMask, "2611", "Iuxxy3$rud15")]
    [InlineData("Old Site", "http://example.org", "me", DefaultMask, "2611", "Xipxr9[mxi91")]
    [InlineData("PayPal", "WWW.PayPal.com", "ed", DefaultMask, "2611", "Hggpe8_fvq80")]
    [InlineData("Httpbin", "httpbin.org", "me", DefaultMask, "2611", "Egtku9-cfo98")]
    [InlineData("Café", "https://café.example", "josé", DefaultMask, "2611", "Iuenu3{gbu25")]
    [InlineData("Bank", "https://bank.example", "ed", "Xxx-NNN.x", "2611", "Haj-814.w")]
    [InlineData("Long", "https://long.example", "me", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", "2611", "ilfatcuwqmrjhrcjasefmjhhaewksivvwvzbnqgt")]
    public void KnownAccounts_ProduceKnownPasswords(string title, string url, string username, string mask, string dateCode, string expected)
    {
        Assert.Equal(expected, Password(title, url, username, mask, dateCode));
    }

    [Fact]
    public void DifferentLoginsOnTheSameSite_GetDifferentPasswords()
    {
        // v1 gave both of these the same password because only the first 32 bytes counted.
        Assert.Equal("Ahluk3(oqe46", Password("Bhphotovideo", "https://www.bhphotovideo.com", "alice@sample.com"));
        Assert.Equal("Yzlls2>wli85", Password("Bhphotovideo", "https://www.bhphotovideo.com", "bob@other.org"));
    }

    [Fact]
    public void FieldsCannotRunTogether()
    {
        // v1 gave ("ab", "", "c") and ("a", "", "bc") the same password.
        Assert.Equal("Snapz9[amp36", Password("ab", "", "c"));
        Assert.Equal("Yreib0$zwl99", Password("a", "", "bc"));
    }

    [Fact]
    public void LongMask_DoesNotRepeat()
    {
        string password = Password("Long", "https://long.example", "me", new string('x', 40));

        Assert.NotEqual(password[..8], password[32..]);
    }

    [Fact]
    public void ChangingTheMask_GivesAnUnrelatedPassword()
    {
        Assert.Equal("hzfqhyct", Password("Masks", "https://masks.example", "me", "xxxxxxxx"));
        Assert.Equal("gxsjunbz69", Password("Masks", "https://masks.example", "me", "xxxxxxxxNN"));
    }

    [Fact]
    public void AccentsTypedEitherWay_GiveTheSamePassword()
    {
        // "é" as one character (U+00E9) or as "e" + combining accent (U+0301).
        Assert.Equal(
            Password("Café", "https://café.example", "josé"),
            Password("Café", "https://café.example", "josé"));
    }

    [Fact]
    public void Csv_ProducesNormalizedFieldsAndPasswords()
    {
        string[] lines = Run(new[]
        {
            Row("Paramount+", "paramountplus.com", "email@yahoo.com"),
            Row("  Padded  ", "  example.com  ", "  user  "),
            "\"  Padded  \",\"  example.com  \",\"  user  \",,,false,false,,",
            "\"Smith, John Bank\",https://bank.example,john,,,false,false,,",
            Row("Café", "https://café.example", "josé"),
        });

        Assert.Equal(new[]
        {
            OutputHeader,
            "Paramount+,https://paramountplus.com,email@yahoo.com,Xxkch8{gnc91",
            "Padded,https://example.com,user,Mbcza0=lmb30",
            "Padded,https://example.com,user,Mbcza0=lmb30",
            "\"Smith, John Bank\",https://bank.example,john,Ykiid5=rrm94",
            "Café,https://café.example,josé,Iuenu3{gbu25",
        }, lines);
    }

    [Fact]
    public void SampleInputFile_ProducesKnownOutput()
    {
        // Solterra's "passwordmask=" note is not applied yet, so it gets the default mask.
        string sample = Path.Combine(AppContext.BaseDirectory, "TestData", "sample_input.csv");
        string[] rows = File.ReadAllLines(sample).Skip(1).ToArray();

        Assert.Equal(new[]
        {
            OutputHeader,
            "Bhphotovideo,https://www.bhphotovideo.com,email@sample.com,Fiipc3]jpv38",
            "Paramount+,https://paramountplus.com,email@yahoo.com,Xxkch8{gnc91",
            "Solterra,,email@sample.com,Iuxxy3$rud15",
            "MTA eTix,https://www.mta.info/,email@sample.com,Qlljp5[oht46",
        }, Run(rows));
    }
}
