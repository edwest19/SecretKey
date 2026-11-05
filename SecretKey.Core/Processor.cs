// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic), replacing an
// earlier version. SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SecretKey.Core;

/// <summary>What a run did, for the command-line and desktop apps to report.</summary>
/// <param name="Accounts">Accounts in the output file.</param>
/// <param name="CustomMasks">Accounts that used their own passwordmask= note.</param>
/// <param name="InputPasswordsCleared">True if old passwords were found and cleared from the input file.</param>
public sealed record ProcessResult(int Accounts, int CustomMasks, bool InputPasswordsCleared);

/// <summary>
/// Turns the input CSV into the output CSV of passwords (README.md "Input file" and "Output file").
/// Every check happens before anything is written: if the input or a setting is wrong,
/// no file is created or changed.
/// </summary>
public static class Processor
{
    /// <summary>Columns the input CSV must have (matched by name, any order, any letter case).</summary>
    public static readonly string[] RequiredColumns = { "Title", "Url", "Username" };

    public const string NotesColumn = "Notes";
    public const string PasswordColumn = "Password";
    public const string OutputHeader = "Title,Url,Username,Password";

    private static readonly Regex MaskDirective =
        new(@"(?<![A-Za-z0-9_])passwordmask=(\S*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

    /// <summary>
    /// Runs with the settings from config.json: checks everything, derives the month's key and
    /// processes the input file. Relative paths are taken relative to <paramref name="configFolder"/>.
    /// </summary>
    public static ProcessResult Run(SecretKeyConfig config, string configFolder, string dateCode)
    {
        ArgumentNullException.ThrowIfNull(config);

        string? error = Validation.DateCodeError(dateCode)
            ?? Validation.RootKeyError(config.RootKey)
            ?? Validation.MaskError(config.PasswordMask);
        if (error is not null)
        {
            throw new ArgumentException(error);
        }
        if (string.IsNullOrWhiteSpace(config.InputPath))
        {
            throw new ArgumentException("InputPath is empty in config.json.");
        }
        if (string.IsNullOrWhiteSpace(config.OutputPath))
        {
            throw new ArgumentException("OutputPath is empty in config.json.");
        }

        string input = Path.GetFullPath(config.InputPath, configFolder);
        string output = Path.GetFullPath(config.OutputPath, configFolder);
        if (string.Equals(input, output, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("OutputPath must be a different file from InputPath.");
        }

        byte[] monthlyKey = Crypto.DeriveMonthlyMasterKey(config.RootKey, dateCode);
        try
        {
            return Process(input, output, monthlyKey, config.PasswordMask);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(monthlyKey);
        }
    }

    /// <summary>
    /// Reads the input CSV, writes the output CSV, then clears any old passwords from the input file.
    /// Throws <see cref="ArgumentException"/> for a bad mask, <see cref="FileNotFoundException"/> for a
    /// missing input file and <see cref="InvalidDataException"/> for a problem in the input file.
    /// </summary>
    public static ProcessResult Process(string inputPath, string outputPath, byte[] monthlyMasterKey, string passwordMask)
    {
        ArgumentNullException.ThrowIfNull(monthlyMasterKey);

        string? maskError = Validation.MaskError(passwordMask);
        if (maskError is not null)
        {
            throw new ArgumentException(maskError);
        }
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input file not found: {inputPath}", inputPath);
        }

        // 1. Read and check everything.
        byte[] raw = File.ReadAllBytes(inputPath);
        bool hasBom = raw.AsSpan().StartsWith(Utf8Bom);
        string text;
        try
        {
            text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(raw, hasBom ? 3 : 0, raw.Length - (hasBom ? 3 : 0));
        }
        catch (DecoderFallbackException)
        {
            throw new InvalidDataException("The input file is not UTF-8 text. Export it again as CSV (UTF-8).");
        }

        List<CsvRecord> records = Csv.Parse(text);
        if (records.Count == 0)
        {
            throw new InvalidDataException("The input file is empty.");
        }

        Dictionary<string, int> columns = ReadHeader(records[0]);
        int title = columns[RequiredColumns[0]], url = columns[RequiredColumns[1]], username = columns[RequiredColumns[2]];
        int notes = columns.GetValueOrDefault(NotesColumn, -1);
        int password = columns.GetValueOrDefault(PasswordColumn, -1);
        int width = records[0].Fields.Count;

        var outputLines = new List<string> { OutputHeader };
        int customMasks = 0;
        bool hadPasswords = false;

        foreach (CsvRecord record in records.Skip(1))
        {
            List<string> fields = record.Fields;
            if (fields.All(string.IsNullOrWhiteSpace))
            {
                continue; // blank line
            }
            if (fields.Count > width)
            {
                throw new InvalidDataException($"Line {record.Line}: this row has {fields.Count} fields but the header has {width}.");
            }
            while (fields.Count < width)
            {
                fields.Add(string.Empty);
            }

            string t = Crypto.NormalizeField(fields[title]);
            string u = Crypto.NormalizeUrl(fields[url]);
            string n = Crypto.NormalizeField(fields[username]);
            if (t.Length == 0 && u.Length == 0 && n.Length == 0)
            {
                throw new InvalidDataException($"Line {record.Line}: this row has no Title, Url or Username.");
            }

            string mask = passwordMask;
            if (notes >= 0)
            {
                (string? customMask, string? error) = FindMaskInNotes(fields[notes]);
                if (error is not null)
                {
                    throw new InvalidDataException($"Line {record.Line} ({Describe(t, u, n)}): {error}");
                }
                if (customMask is not null)
                {
                    mask = customMask;
                    customMasks++;
                }
            }

            outputLines.Add(Csv.FormatRecord(new[] { t, u, n, Crypto.GeneratePassword(monthlyMasterKey, t, u, n, mask) }));

            if (password >= 0 && fields[password].Length > 0)
            {
                fields[password] = string.Empty;
                hadPasswords = true;
            }
        }

        // 2. Write the output file.
        string? outputFolder = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        File.WriteAllLines(outputPath, outputLines);

        // 3. Clear old passwords from the input file, keeping every other value. Written to a
        //    temporary file first and then swapped in, so a failure can't leave a half-written file.
        if (hadPasswords)
        {
            string rewritten = string.Join("\r\n", records.Select(r => Csv.FormatRecord(r.Fields))) + "\r\n";
            byte[] body = Encoding.UTF8.GetBytes(rewritten);
            string temp = inputPath + ".secretkey-tmp";
            File.WriteAllBytes(temp, hasBom ? Utf8Bom.Concat(body).ToArray() : body);
            File.Move(temp, inputPath, overwrite: true);

            Logger.LogSanitization(Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? AppContext.BaseDirectory, outputPath, outputLines.Count - 1);
        }

        return new ProcessResult(outputLines.Count - 1, customMasks, hadPasswords);
    }

    /// <summary>
    /// Finds a passwordmask= setting in an account's Notes (README.md "Per-account masks").
    /// Returns (null, null) when there is none, (mask, null) when it is valid, or (null, message) when it is not.
    /// </summary>
    public static (string? Mask, string? Error) FindMaskInNotes(string? notes)
    {
        MatchCollection matches = MaskDirective.Matches(notes ?? string.Empty);
        if (matches.Count == 0)
        {
            return (null, null);
        }
        if (matches.Count > 1)
        {
            return (null, "Notes contain more than one passwordmask= setting.");
        }

        string mask = matches[0].Groups[1].Value;
        string? error = Validation.MaskError(mask);
        return error is null ? (mask, null) : (null, "passwordmask=" + mask + ": " + error);
    }

    private static Dictionary<string, int> ReadHeader(CsvRecord header)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Fields.Count; i++)
        {
            string name = header.Fields[i].Trim();
            if (name.Length > 0 && !columns.TryAdd(name, i))
            {
                throw new InvalidDataException($"The header has the column \"{name}\" more than once.");
            }
        }

        string[] missing = RequiredColumns.Where(c => !columns.ContainsKey(c)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException(
                $"The header is missing the column(s) {string.Join(", ", missing)}. The input needs Title, Url and Username columns (any order).");
        }
        return columns;
    }

    private static string Describe(string title, string url, string username) =>
        title.Length > 0 ? title : url.Length > 0 ? url : username;
}
