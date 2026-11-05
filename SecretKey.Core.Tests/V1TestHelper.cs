using SecretKey.Core;

namespace SecretKey.Core.Tests;

/// <summary>
/// Shared setup for the v1 known-answer tests.
/// Every expected value in this project was produced by the v1.0.0 engine and
/// cross-checked against an independent implementation of the same algorithm.
/// </summary>
internal static class V1TestHelper
{
    // TEST-ONLY root key. Never use this for real passwords.
    public const string TestRootKey = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F";

    public const string DefaultMask = "XxxxxNSxxxNN";
    public const string DateCode = "2611";

    public const string InputHeader = "Title,Url,Username,Password,OTPAuth,Favorite,Archived,Tags,Notes";
    public const string OutputHeader = "Title,Url,Username,Password";

    /// <summary>Builds an unquoted input row in the 1Password-style column order the engine reads.</summary>
    public static string Row(string title, string url, string username) =>
        $"{title},{url},{username},,,false,false,,";

    /// <summary>
    /// Runs the real engine on the given input rows in a private temp folder and returns the output lines.
    /// (The engine rewrites its input file in place, so it must never be pointed at a shared file.)
    /// </summary>
    public static string[] Run(IEnumerable<string> inputRows, string mask = DefaultMask, string dateCode = DateCode)
    {
        string dir = Path.Combine(Path.GetTempPath(), "secretkey-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string input = Path.Combine(dir, "input.csv");
            string output = Path.Combine(dir, "output.csv");
            File.WriteAllLines(input, new[] { InputHeader }.Concat(inputRows));

            Processor.Process(input, output, Crypto.DeriveMonthlyMasterKey(TestRootKey, dateCode), mask);

            return File.ReadAllLines(output);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Runs one account through the engine and returns its output line.</summary>
    public static string OutputLineFor(string title, string url, string username, string mask = DefaultMask, string dateCode = DateCode)
    {
        string[] lines = Run(new[] { Row(title, url, username) }, mask, dateCode);
        Assert.Equal(2, lines.Length);
        Assert.Equal(OutputHeader, lines[0]);
        return lines[1];
    }
}
