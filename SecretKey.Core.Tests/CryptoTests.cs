using System.Text;
using SecretKey.Core;

namespace SecretKey.Core.Tests;

/// <summary>
/// Known-answer tests for the building blocks in Crypto.cs.
/// If any of these fail, previously generated passwords can no longer be reproduced.
/// </summary>
public class CryptoTests
{
    [Fact]
    public void MonthlyMasterKey_IsSha256OfRootKeyTextPlusDateCode()
    {
        byte[] key = Crypto.DeriveMonthlyMasterKey(V1TestHelper.TestRootKey, "2611");

        Assert.Equal("4E0F0B5B77491DCA67A584A58EC1F6D4859775346265F8C03B51878FE2CF8A12", Convert.ToHexString(key));
    }

    [Fact]
    public void HmacSha256_MatchesRfc4231TestCase2()
    {
        byte[] mac = Crypto.HmacSha256(Encoding.ASCII.GetBytes("Jefe"), Encoding.ASCII.GetBytes("what do ya want for nothing?"));

        Assert.Equal("5BDCC146BF60754E6A042426089575C75A003F089D2739839DEC58B964EC3843", Convert.ToHexString(mac));
    }

    [Fact]
    public void HmacSha256_OfAccountBlock_IsStable()
    {
        byte[] key = Crypto.DeriveMonthlyMasterKey(V1TestHelper.TestRootKey, "2611");
        var block = new byte[32]; // v1: title + url + username, UTF-8, cut or zero-padded to 32 bytes
        byte[] text = Encoding.UTF8.GetBytes("Bank" + "https://bank.example" + "ed");
        Array.Copy(text, block, text.Length);

        Assert.Equal("0F803F563CA1F44777139CCC4BB04B81DA0373509D3D28FC76E896BB234AD5C9", Convert.ToHexString(Crypto.HmacSha256(key, block)));
    }

    [Fact]
    public void MapBlobToPattern_MapsEachMaskTokenFromOneHashByte()
    {
        byte[] bytes = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

        // X -> A-Z, x -> a-z, N -> 0-9, S -> "!@#$%&*()-_=+[]{}<>?", anything else is copied as-is.
        // A literal still uses up a hash byte (v1 behavior).
        Assert.Equal("Ab2$-Fg7)", Crypto.MapBlobToPattern(bytes, "XxNS-XxNS"));
    }

    [Fact]
    public void MapBlobToPattern_WrapsAfter32Characters_V1Behavior()
    {
        byte[] bytes = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

        // Known v1 weakness: mask positions 33+ reuse the same 32 hash bytes.
        Assert.Equal("0123456789012345678901234567890101234567", Crypto.MapBlobToPattern(bytes, new string('N', 40)));
    }
}
