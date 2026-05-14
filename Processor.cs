using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public static class Processor
{
    // Process the input CSV and write output CSV (appending Password column)
    public static void Process(string inputPath, string outputPath, byte[] monthlyMasterKey, int passwordLength = 16)
    {
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Input file not found: {inputPath}");
            return;
        }

        var lines = File.ReadAllLines(inputPath);
        if (lines.Length == 0)
        {
            Console.WriteLine("Input CSV is empty.");
            return;
        }

        // Parse header and prepare output header
        string header = lines[0];
        string outHeader = header + ",Password";

        var outLines = new List<string> { outHeader };

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = ParseCsvLine(line);
            // Expect at least 4 columns: Title,Website,Username,Password Regex
            if (fields.Count < 4)
            {
                Console.WriteLine($"Skipping malformed row {i + 1}: {line}");
                continue;
            }

            string title = fields[0];
            string website = fields[1];
            string username = fields[2];
            string passwordRegex = fields[3];

            // Create deterministic 32-byte block from Title+Website+Username
            byte[] block = CreateFixedBlock(title, website, username);

            // Initial HMAC
            byte[] hash = Crypto.HmacSha256(monthlyMasterKey, block);

            string password = Crypto.HashToPassword(hash, passwordLength);

            // Compile regex
            Regex regex;
            try
            {
                regex = new Regex(passwordRegex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid regex on row {i + 1}: {ex.Message}");
                continue;
            }

            int attempts = 0;
            const int maxAttempts = 10000;
            while (!regex.IsMatch(password) && attempts < maxAttempts)
            {
                // Deterministic re-hash: HMAC of previous hash bytes
                hash = Crypto.HmacSha256(monthlyMasterKey, hash);
                password = Crypto.HashToPassword(hash, passwordLength);
                attempts++;
            }

            if (attempts >= maxAttempts)
            {
                Console.WriteLine($"Failed to generate password matching regex on row {i + 1} after {maxAttempts} attempts.");
            }

            // Reconstruct output line: keep original raw fields and append password (quoted if needed)
            string outLine = line + "," + QuoteIfNeeded(password);
            outLines.Add(outLine);
        }

        // Ensure output directory exists
        var outDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        File.WriteAllLines(outputPath, outLines);
        Console.WriteLine($"Processed {outLines.Count - 1} rows. Output written to {outputPath}");
    }

    // Simple CSV parser for a single line supporting quoted fields
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        if (line == null) return fields;

        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    sb.Append('"');
                    i++; // skip next
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        fields.Add(sb.ToString());
        return fields;
    }

    // Create a deterministic 32-byte block from the concatenation of title, website, and username.
    private static byte[] CreateFixedBlock(string title, string website, string username)
    {
        string combined = (title ?? "") + (website ?? "") + (username ?? "");
        var bytes = Encoding.UTF8.GetBytes(combined);
        const int size = 32;
        var block = new byte[size];

        // If bytes shorter than size, copy and pad with deterministic pattern (0x00.. then length)
        if (bytes.Length >= size)
        {
            Array.Copy(bytes, block, size);
        }
        else
        {
            Array.Copy(bytes, block, bytes.Length);
            // Deterministic padding: fill remaining with repeated 0xA5 pattern
            for (int i = bytes.Length; i < size; i++) block[i] = 0xA5;
        }

        return block;
    }

    private static string QuoteIfNeeded(string s)
    {
        if (s == null) return "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
        {
            return '"' + s.Replace("\"", "\"\"") + '"';
        }
        return s;
    }
}
