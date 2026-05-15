// AI Assistant Acknowledgement: This file was created or modified with assistance from an AI programming assistant named "GitHub Copilot".
// Review generated code before use and treat any embedded secrets appropriately.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public static class Processor
{
    // Process the input CSV and write output CSV (appending Password column)
    public static void Process(string inputPath, string outputPath, byte[] monthlyMasterKey, int passwordLength = 16, string passwordMask = "XxxxxNSxxxNN")
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
        var headerFields = ParseCsvLine(header);

        // Determine column indices by header names (case-insensitive). Accept URL or Website, and Regex or Password Regex.
        int idxTitle = 0, idxWebsite = 1, idxUsername = 2, idxRegex = 3;
        bool headerMapped = false;
        if (headerFields.Count >= 4)
        {
            for (int hi = 0; hi < headerFields.Count; hi++)
            {
                var name = headerFields[hi].Trim().ToLowerInvariant();
                if (name == "title") idxTitle = hi;
                else if (name == "website" || name == "url") idxWebsite = hi;
                else if (name == "username") idxUsername = hi;
                else if (name == "password regex" || name == "passwordregex" || name == "regex") idxRegex = hi;
            }
            headerMapped = true;
        }

        string outHeader = header + ",Password";
        var outLines = new List<string> { outHeader };

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = ParseCsvLine(line);
            // Expect at least 4 columns
            if (fields.Count <= Math.Max(Math.Max(idxTitle, idxWebsite), Math.Max(idxUsername, idxRegex)))
            {
                Console.WriteLine($"Skipping malformed row {i + 1}: {line}");
                continue;
            }

            string title = fields.Count > idxTitle ? fields[idxTitle] : string.Empty;
            string website = fields.Count > idxWebsite ? fields[idxWebsite] : string.Empty;
            string username = fields.Count > idxUsername ? fields[idxUsername] : string.Empty;
            string passwordRegex = fields.Count > idxRegex ? fields[idxRegex] : string.Empty;

            // Create deterministic 32-byte block from Title+Website+Username
            byte[] block = CreateFixedBlock(title, website, username);

            // Initial HMAC
            byte[] hash = Crypto.HmacSha256(monthlyMasterKey, block);

            // Map HMAC to password using configured mask and length
            // Note: mask controls structure; passwordLength is informational when mask contains literals
            string password = Crypto.MapBlobToPattern(hash, passwordMask);

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
