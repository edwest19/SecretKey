using static SecretKey.Core.Tests.V1TestHelper;

namespace SecretKey.Core.Tests;

/// <summary>
/// Locks the passwords SecretKey v1.0.0 produces, end to end (CSV in, CSV out).
///
/// Why this exists: the whole point of SecretKey is that the same root key and date code
/// give the same passwords forever. If any test here fails, a code change has altered
/// users' passwords. Do not "fix" the expected values to make a test pass — change the
/// code back, or put the new behavior behind an explicit, opt-in new version.
/// </summary>
public class PasswordDerivationV1Tests
{
    [Theory]
    [InlineData("Bhphotovideo", "https://www.bhphotovideo.com", "email@sample.com", DefaultMask, "2611",
        "Bhphotovideo,https://www.bhphotovideo.com,email@sample.com,Wocqe8<tvg04")]
    [InlineData("Bhphotovideo", "https://www.bhphotovideo.com", "email@sample.com", DefaultMask, "2612",
        "Bhphotovideo,https://www.bhphotovideo.com,email@sample.com,Itjak8_enx59")]
    [InlineData("Paramount+", "paramountplus.com", "email@yahoo.com", DefaultMask, "2611",
        "Paramount+,https://paramountplus.com,email@yahoo.com,Gmrdj4<hdz14")]
    [InlineData("Paramount+", "paramountplus.com", "email@yahoo.com", "xxxxxxxxNNNN", "2611",
        "Paramount+,https://paramountplus.com,email@yahoo.com,gmrdjaph7914")]
    [InlineData("MTA eTix", "https://www.mta.info/", "email@sample.com", DefaultMask, "2611",
        "MTA eTix,https://www.mta.info/,email@sample.com,Zuzhl4]zit82")]
    [InlineData("Solterra", "", "email@sample.com", DefaultMask, "2611",
        "Solterra,,email@sample.com,Qbxml3&dlt19")]
    [InlineData("Old Site", "http://example.org", "me", DefaultMask, "2611",
        "Old Site,http://example.org,me,Kvxan9)vvg19")]
    [InlineData("PayPal", "WWW.PayPal.com", "ed", DefaultMask, "2611",
        "PayPal,https://WWW.PayPal.com,ed,Enwxh1@lfp63")] // URL letter case is kept as typed
    [InlineData("Café", "https://café.example", "josé", DefaultMask, "2611",
        "Café,https://café.example,josé,Skyhq4<kuy09")]
    [InlineData("Bank", "https://bank.example", "ed", "Xxx-NNN.x", "2611",
        "Bank,https://bank.example,ed,Pyl-014.p")]
    public void KnownAccounts_ProduceKnownPasswords(string title, string url, string username, string mask, string dateCode, string expectedOutputLine)
    {
        Assert.Equal(expectedOutputLine, OutputLineFor(title, url, username, mask, dateCode));
    }

    [Fact]
    public void UrlWithoutScheme_GetsHttpsPrefix_ButEmptyUrlStaysEmpty()
    {
        Assert.StartsWith("Paramount+,https://paramountplus.com,", OutputLineFor("Paramount+", "paramountplus.com", "email@yahoo.com"));
        Assert.StartsWith("Solterra,,", OutputLineFor("Solterra", "", "email@sample.com"));
    }

    [Fact]
    public void SpacesAroundFields_AreIgnored()
    {
        string padded = OutputLineFor("  Padded  ", "  example.com  ", "  user  ");
        string paddedInsideQuotes = Run(new[] { "\"  Padded  \",\"  example.com  \",\"  user  \",,,false,false,," })[1];
        string clean = OutputLineFor("Padded", "example.com", "user");

        Assert.Equal("Padded,https://example.com,user,Kgbqd5&vtr41", clean);
        Assert.Equal(clean, padded);
        Assert.Equal(clean, paddedInsideQuotes);
    }

    [Fact]
    public void QuotedTitleContainingComma_IsReadAsOneField()
    {
        string[] lines = Run(new[] { "\"Smith, John Bank\",https://bank.example,john,,,false,false,," });

        Assert.Equal("\"Smith, John Bank\",https://bank.example,john,Hmxkl2-ual91", lines[1]);
    }

    [Fact]
    public void UsernameIsIgnored_WhenTitlePlusUrlFill32Bytes_V1Behavior()
    {
        // Known v1 weakness: only the first 32 bytes of title+url+username are used.
        // "Bhphotovideo" + "https://www.bhphotovideo.com" is already 40 bytes, so two
        // different logins on this site get the same password.
        string[] lines = Run(new[]
        {
            Row("Bhphotovideo", "https://www.bhphotovideo.com", "alice@sample.com"),
            Row("Bhphotovideo", "https://www.bhphotovideo.com", "bob@other.org"),
        });

        Assert.EndsWith(",Wocqe8<tvg04", lines[1]);
        Assert.EndsWith(",Wocqe8<tvg04", lines[2]);
    }

    [Fact]
    public void FieldsAreJoinedWithoutSeparator_V1Behavior()
    {
        // Known v1 weakness: ("ab", "", "c") and ("a", "", "bc") produce the same input bytes.
        string first = OutputLineFor("ab", "", "c");
        string second = OutputLineFor("a", "", "bc");

        Assert.Equal(first.Split(',')[^1], second.Split(',')[^1]);
    }

    [Fact]
    public void SampleInputFile_ProducesKnownOutput()
    {
        // Solterra's "passwordmask=" note is not applied by v1.0.0, so it gets the default mask.
        string sample = Path.Combine(AppContext.BaseDirectory, "TestData", "sample_input.csv");
        string[] rows = File.ReadAllLines(sample).Skip(1).ToArray();

        Assert.Equal(new[]
        {
            "Title,Url,Username,Password",
            "Bhphotovideo,https://www.bhphotovideo.com,email@sample.com,Wocqe8<tvg04",
            "Paramount+,https://paramountplus.com,email@yahoo.com,Gmrdj4<hdz14",
            "Solterra,,email@sample.com,Qbxml3&dlt19",
            "MTA eTix,https://www.mta.info/,email@sample.com,Zuzhl4]zit82",
        }, Run(rows));
    }
}
