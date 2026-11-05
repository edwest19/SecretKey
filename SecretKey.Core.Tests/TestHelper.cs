using SecretKey.Core;

namespace SecretKey.Core.Tests;

/// <summary>
/// Shared setup for the password tests.
/// Every expected password in this project was produced by the engine and independently
/// re-computed from docs/password-derivation.md by a separate implementation.
/// </summary>
internal static class TestHelper
{
    // TEST-ONLY root key. Never use this for real passwords.
    public const string TestRootKey = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F";

    public const string DefaultMask = "XxxxxNSxxxNN";
    public const string DateCode = "2611";

    public const string InputHeader = "Title,Url,Username,Password,OTPAuth,Favorite,Archived,Tags,Notes";
    public const string OutputHeader = "Title,Url,Username,Password";

    public static byte[] MonthlyKey(string dateCode = DateCode) =>
        Crypto.DeriveMonthlyMasterKey(TestRootKey, dateCode);

    public static string Password(string title, string url, string username, string mask = DefaultMask, string dateCode = DateCode) =>
        Crypto.GeneratePassword(MonthlyKey(dateCode), title, url, username, mask);

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

            Processor.Process(input, output, MonthlyKey(dateCode), mask);

            return File.ReadAllLines(output);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
