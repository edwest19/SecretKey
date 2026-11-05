using SecretKey.Core;
using static SecretKey.Core.Tests.TestHelper;

namespace SecretKey.Core.Tests;

/// <summary>
/// Tests for the building blocks of the password math (docs/password-derivation.md).
/// </summary>
public class CryptoTests
{
    [Fact]
    public void MonthlyKey_IsKnownValue()
    {
        Assert.Equal("D38F4A15A5783BA78B9A80FF2D2AA0FA8086E6AB88D33C9A20206978658A3BB6", Convert.ToHexString(MonthlyKey("2611")));
        Assert.Equal("A516AF24CE369E121466A7FD928EC7170CC44590FC6DAAC0E1B77DFD326C9702", Convert.ToHexString(MonthlyKey("2612")));
    }

    [Fact]
    public void RootKey_IgnoresLetterCaseAndSurroundingSpaces()
    {
        byte[] expected = MonthlyKey("2611");

        Assert.Equal(expected, Crypto.DeriveMonthlyMasterKey(TestRootKey.ToLowerInvariant(), "2611"));
        Assert.Equal(expected, Crypto.DeriveMonthlyMasterKey("  " + TestRootKey + "\n", "2611"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("REPLACE_WITH_YOUR_ROOT_KEY")]
    [InlineData("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1")]    // 63 characters
    [InlineData("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F00")]  // 66 characters
    [InlineData("G00102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")]    // not hexadecimal
    public void RootKey_ThatIsNot32HexBytes_IsRejected(string badKey)
    {
        Assert.Throws<ArgumentException>(() => Crypto.DeriveMonthlyMasterKey(badKey, "2611"));
    }

    [Fact]
    public void EncodeFields_WritesBigEndianLengthThenUtf8()
    {
        Assert.Equal(
            new byte[] { 0, 0, 0, 1, (byte)'a', 0, 0, 0, 0, 0, 0, 0, 2, 0xC3, 0xA9 },
            Crypto.EncodeFields("a", "", "é"));
    }

    [Theory]
    [InlineData("X", new byte[] { 234, 255, 0 }, "A")]    // 234 and 255 are skipped for 26-letter sets
    [InlineData("x", new byte[] { 233 }, "z")]            // 233 is the highest accepted byte: 233 mod 26 = 25
    [InlineData("N", new byte[] { 250, 249 }, "9")]       // digits accept bytes below 250
    [InlineData("S", new byte[] { 240, 239 }, "?")]       // symbols accept bytes below 240
    [InlineData("X-x.N", new byte[] { 0, 1, 2 }, "A-b.2")] // other characters are copied and use no bytes
    public void MapStreamToMask_SkipsBiasedBytes(string mask, byte[] stream, string expected)
    {
        Assert.Equal(expected, Crypto.MapStreamToMask(stream, mask));
    }

    [Fact]
    public void MapStreamToMask_FailsLoudlyIfStreamRunsOut()
    {
        Assert.Throws<InvalidOperationException>(() => Crypto.MapStreamToMask(new byte[] { 255 }, "x"));
    }

    [Fact]
    public void NormalizeUrl_AddsHttpsOnlyWhenNoScheme()
    {
        Assert.Equal("", Crypto.NormalizeUrl("  "));
        Assert.Equal("https://paramountplus.com", Crypto.NormalizeUrl(" paramountplus.com "));
        Assert.Equal("https://httpbin.org", Crypto.NormalizeUrl("httpbin.org"));
        Assert.Equal("http://example.org", Crypto.NormalizeUrl("http://example.org"));
        Assert.Equal("HTTPS://Example.com", Crypto.NormalizeUrl("HTTPS://Example.com"));
    }
}
