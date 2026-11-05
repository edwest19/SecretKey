// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text;
using SecretKey.Core;
using static SecretKey.Core.Tests.TestHelper;

namespace SecretKey.Core.Tests;

/// <summary>
/// Tests for reading the input file, writing the output file, clearing old passwords,
/// per-account masks and settings (README.md "Input file", "Output file", "Per-account masks").
/// </summary>
[TestClass]
public sealed class ProcessorTests
{
    private string dir = "";

    [TestInitialize]
    public void CreateFolder()
    {
        dir = Path.Combine(Path.GetTempPath(), "secretkey-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
    }

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(dir, recursive: true);

    private string Input => Path.Combine(dir, "input.csv");
    private string Output => Path.Combine(dir, "output.csv");

    private ProcessResult Process(string inputText, string mask = DefaultMask)
    {
        File.WriteAllText(Input, inputText);
        return Processor.Process(Input, Output, MonthlyKey(), mask);
    }

    private void AssertNothingWritten(string originalInput)
    {
        Assert.IsFalse(File.Exists(Output), "The output file should not have been created.");
        Assert.AreEqual(originalInput, File.ReadAllText(Input), "The input file should not have changed.");
    }

    [TestMethod]
    public void Columns_AreFoundByName_InAnyOrderAndLetterCase()
    {
        // The column order the old README listed: title, username, password, url, notes.
        Process("title,USERNAME,password,Url,notes\nParamount+,email@yahoo.com,,paramountplus.com,\n");

        CollectionAssert.AreEqual(new[]
        {
            OutputHeader,
            "Paramount+,https://paramountplus.com,email@yahoo.com,Xxkch8{gnc91",
        }, File.ReadAllLines(Output));
    }

    [TestMethod]
    public void MissingRequiredColumn_StopsTheRun()
    {
        const string text = "Title,Username,Password\nSite,me,\n";
        var ex = Assert.ThrowsExactly<InvalidDataException>(() => Process(text));
        Assert.Contains("Url", ex.Message);
        AssertNothingWritten(text);
    }

    [TestMethod]
    public void RepeatedColumn_StopsTheRun()
    {
        const string text = "Title,Url,Username,url\nSite,a.com,me,b.com\n";
        Assert.ThrowsExactly<InvalidDataException>(() => Process(text));
        AssertNothingWritten(text);
    }

    [TestMethod]
    public void PasswordMaskInNotes_IsUsedForThatAccountOnly()
    {
        ProcessResult result = Process(
            "Title,Url,Username,Notes\n" +
            "Paramount+,paramountplus.com,email@yahoo.com,\"Streaming\nPASSWORDMASK=xxxxxxxxNNNN no symbols allowed\"\n" +
            "Bhphotovideo,https://www.bhphotovideo.com,email@sample.com,Shopping\n");

        CollectionAssert.AreEqual(new[]
        {
            OutputHeader,
            "Paramount+,https://paramountplus.com,email@yahoo.com,hleziocv3322",
            "Bhphotovideo,https://www.bhphotovideo.com,email@sample.com,Fiipc3]jpv38",
        }, File.ReadAllLines(Output));
        Assert.AreEqual(new ProcessResult(2, 1, false), result);
    }

    [TestMethod]
    public void PasswordMaskInsideAnotherWord_IsNotASetting()
    {
        Process("Title,Url,Username,Notes\nParamount+,paramountplus.com,email@yahoo.com,mypasswordmask=xxxxxxxxNNNN\n");

        Assert.AreEqual("Paramount+,https://paramountplus.com,email@yahoo.com,Xxkch8{gnc91", File.ReadAllLines(Output)[1]);
    }

    [TestMethod]
    [DataRow("passwordmask=abc")]                           // fewer than 4 of X, x, N, S
    [DataRow("passwordmask=")]                              // empty
    [DataRow("passwordmask=XxxxNN passwordmask=NNNNNN")]    // two settings
    public void InvalidPasswordMaskInNotes_StopsTheRun_NamingTheLineAndAccount(string notes)
    {
        string text = "Title,Url,Username,Password,Notes\nGood,a.com,me,old1,\nBank,b.com,me,old2," + notes + "\n";
        var ex = Assert.ThrowsExactly<InvalidDataException>(() => Process(text));
        Assert.Contains("Line 3", ex.Message);
        Assert.Contains("Bank", ex.Message);
        AssertNothingWritten(text);
    }

    [TestMethod]
    public void OldPasswords_AreClearedFromTheInput_KeepingEverythingElse()
    {
        byte[] bom = { 0xEF, 0xBB, 0xBF };
        string original =
            "Title,Url,Username,Password,Notes\r\n" +
            "Bank,https://bank.example,ed,OldSecret,\"line one\r\nline two, with a comma\"\r\n" +
            "Mail,https://mail.example,ed,,\r\n";
        File.WriteAllBytes(Input, bom.Concat(Encoding.UTF8.GetBytes(original)).ToArray());

        ProcessResult result = Processor.Process(Input, Output, MonthlyKey(), DefaultMask);

        byte[] after = File.ReadAllBytes(Input);
        CollectionAssert.AreEqual(bom, after.Take(3).ToArray(), "The byte-order mark should be kept.");
        Assert.AreEqual(original.Replace("OldSecret", ""), Encoding.UTF8.GetString(after, 3, after.Length - 3));
        Assert.IsTrue(result.InputPasswordsCleared);
        Assert.IsFalse(File.Exists(Input + ".secretkey-tmp"));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "secretkey_events.log")));
    }

    [TestMethod]
    public void InputWithoutPasswords_IsNotTouched()
    {
        // Written with \n line endings and an unnecessarily quoted field: any rewrite would change these bytes.
        const string original = "Title,Url,Username,Password\n\"Bank\",bank.example,ed,\n";

        ProcessResult result = Process(original);

        Assert.AreEqual(original, File.ReadAllText(Input));
        Assert.IsFalse(result.InputPasswordsCleared);
        Assert.IsFalse(File.Exists(Path.Combine(dir, "secretkey_events.log")));
    }

    [TestMethod]
    public void BlankLines_AreSkipped()
    {
        ProcessResult result = Process("Title,Url,Username\n\nSolterra,,email@sample.com\n\n");

        Assert.AreEqual(1, result.Accounts);
        Assert.AreEqual("Solterra,,email@sample.com,Iuxxy3$rud15", File.ReadAllLines(Output)[1]);
    }

    [TestMethod]
    public void RowWithTooManyFields_StopsTheRun()
    {
        const string text = "Title,Url,Username\nA,b.com,me\nB,c.com,me,extra\n";
        var ex = Assert.ThrowsExactly<InvalidDataException>(() => Process(text));
        Assert.Contains("Line 3", ex.Message);
        AssertNothingWritten(text);
    }

    [TestMethod]
    public void RowWithNoTitleUrlOrUsername_StopsTheRun()
    {
        const string text = "Title,Url,Username,Notes\n,,,only a note\n";
        var ex = Assert.ThrowsExactly<InvalidDataException>(() => Process(text));
        Assert.Contains("Line 2", ex.Message);
        AssertNothingWritten(text);
    }

    [TestMethod]
    public void InputThatIsNotUtf8_StopsTheRun()
    {
        File.WriteAllBytes(Input, new byte[] { (byte)'T', (byte)'i', 0xFF, 0xFE, (byte)'\n' });
        Assert.ThrowsExactly<InvalidDataException>(() => Processor.Process(Input, Output, MonthlyKey(), DefaultMask));
        Assert.IsFalse(File.Exists(Output));
    }

    [TestMethod]
    public void InvalidGlobalMask_StopsTheRun()
    {
        const string text = "Title,Url,Username\nA,b.com,me\n";
        Assert.ThrowsExactly<ArgumentException>(() => Process(text, mask: "Password1!"));
        AssertNothingWritten(text);
    }

    [TestMethod]
    public void Run_ResolvesRelativePathsAgainstTheConfigFolder()
    {
        File.WriteAllText(Input, "Title,Url,Username\nSolterra,,email@sample.com\n");
        var config = new SecretKeyConfig { RootKey = TestRootKey, InputPath = "input.csv", OutputPath = "out/passwords.csv" };

        ProcessResult result = Processor.Run(config, dir, DateCode);

        Assert.AreEqual(1, result.Accounts);
        Assert.AreEqual("Solterra,,email@sample.com,Iuxxy3$rud15", File.ReadAllLines(Path.Combine(dir, "out", "passwords.csv"))[1]);
    }

    [TestMethod]
    public void Run_RejectsBadSettingsBeforeWritingAnything()
    {
        File.WriteAllText(Input, "Title,Url,Username\nA,b.com,me\n");
        var good = new SecretKeyConfig { RootKey = TestRootKey, InputPath = "input.csv", OutputPath = "output.csv" };

        Assert.ThrowsExactly<ArgumentException>(() => Processor.Run(good, dir, "2613"));
        Assert.ThrowsExactly<ArgumentException>(() => Processor.Run(Copy(good, c => c.RootKey = "REPLACE_WITH_YOUR_ROOT_KEY"), dir, DateCode));
        Assert.ThrowsExactly<ArgumentException>(() => Processor.Run(Copy(good, c => c.PasswordMask = ""), dir, DateCode));
        Assert.ThrowsExactly<ArgumentException>(() => Processor.Run(Copy(good, c => c.OutputPath = "INPUT.csv"), dir, DateCode));
        Assert.ThrowsExactly<ArgumentException>(() => Processor.Run(Copy(good, c => c.InputPath = ""), dir, DateCode));
        Assert.ThrowsExactly<FileNotFoundException>(() => Processor.Run(Copy(good, c => c.InputPath = "missing.csv"), dir, DateCode));
        Assert.IsFalse(File.Exists(Output));
    }

    [TestMethod]
    public void ConfigExample_LoadsButItsPlaceholderKeyIsRejected()
    {
        SecretKeyConfig example = SecretKeyConfig.Load(Path.Combine(RepoRoot(), "config.example.json"));
        File.Copy(Path.Combine(RepoRoot(), "sample_input.csv"), Path.Combine(dir, example.InputPath));

        var ex = Assert.ThrowsExactly<ArgumentException>(() => Processor.Run(example, dir, DateCode));
        Assert.Contains("root key", ex.Message);
    }

    [TestMethod]
    public void Config_SavesAndLoadsTheSameValues()
    {
        var config = new SecretKeyConfig { RootKey = TestRootKey, InputPath = "in.csv", OutputPath = "out.csv", PasswordMask = "NNNNNN" };
        string path = Path.Combine(dir, SecretKeyConfig.FileName);

        config.Save(path);
        SecretKeyConfig loaded = SecretKeyConfig.Load(path);

        Assert.AreEqual(
            (config.RootKey, config.InputPath, config.OutputPath, config.PasswordMask),
            (loaded.RootKey, loaded.InputPath, loaded.OutputPath, loaded.PasswordMask));
    }

    private static SecretKeyConfig Copy(SecretKeyConfig source, Action<SecretKeyConfig> change)
    {
        var copy = new SecretKeyConfig
        {
            RootKey = source.RootKey,
            InputPath = source.InputPath,
            OutputPath = source.OutputPath,
            PasswordMask = source.PasswordMask,
        };
        change(copy);
        return copy;
    }
}
