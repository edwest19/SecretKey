// AI Assistant Acknowledgement: This file was created or modified with assistance from an AI programming assistant named "GitHub Copilot".
// Review generated code before use and treat any embedded secrets appropriately.
// ============================================================================
// Copyright (c) 2026 edwest19
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// ============================================================================
// AI Assistant Acknowledgement: This file was created or modified with assistance from an AI programming assistant named "GitHub Copilot".
// Review generated code before use and treat any embedded secrets appropriately.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public static class Processor
{
    private static readonly Regex MaskRegex = new Regex(@"passwordmask=([A-Za-zNSs]+)", RegexOptions.Compiled);

    public static void Process(string inputPath, string outputPath, byte[] monthlyMasterKey, string defaultPasswordMask = "XxxxxNSxxxNN")
    {
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Input file not found: {inputPath}");
            return;
        }

        // --- FIX: Read the entire file as a single string block ---
        string fullText = File.ReadAllText(inputPath, Encoding.UTF8);
        var records = ParseFullCsvText(fullText);

        if (records.Count == 0)
        {
            Console.WriteLine("Input CSV is empty.");
            return;
        }

        // The first record is our header row
        var headerFields = records[0];

        // Reconstruct a clean header string for our output file
        var headerBuilder = new StringBuilder();
        for (int h = 0; h < headerFields.Count; h++)
        {
            headerBuilder.Append(QuoteIfNeeded(headerFields[h]));
            if (h < headerFields.Count - 1) headerBuilder.Append(',');
        }

        var outLines = new List<string> { headerBuilder.ToString() };

        const int idxTitle = 0;
        const int idxWebsite = 1;
        const int idxUsername = 2;
        const int idxPassword = 3;
        const int idxNotes = 8;

        // Start loop at 1 to skip the header record
        for (int i = 1; i < records.Count; i++)
        {
            var fields = records[i];

            // Skip purely empty lines at the end of the file
            if (fields.Count == 0 || (fields.Count == 1 && string.IsNullOrWhiteSpace(fields[0]))) continue;

            if (fields.Count < 3)
            {
                // Let's print a clean preview of the malformed data for diagnostics
                string preview = fields.Count > 0 ? fields[0] : "Empty Fragment";
                Console.WriteLine($"Skipping malformed row {i + 1}: {preview.Replace("\r", " ").Replace("\n", " ")}");
                continue;
            }

            string title = fields[idxTitle];
            string website = fields[idxWebsite];
            string username = fields[idxUsername];

            while (fields.Count < 9)
            {
                fields.Add(string.Empty);
            }

            string activeMask = defaultPasswordMask;
            string notesField = fields[idxNotes];

            if (!string.IsNullOrWhiteSpace(notesField))
            {
                var match = MaskRegex.Match(notesField);
                if (match.Success)
                {
                    activeMask = match.Groups[1].Value.Trim();
                }
            }

            website = CleanUrl(website);
            byte[] block = CreateFixedBlock(title, website, username);

            byte[] hash = Crypto.HmacSha256(monthlyMasterKey, block);
            string password = Crypto.MapBlobToPattern(hash, activeMask);

            // Blindly overwrite the password column
            fields[idxPassword] = password;

            // Reconstruct the line preserving multi-line notes inside quotes flawlessly
            var sb = new StringBuilder();
            for (int f = 0; f < fields.Count; f++)
            {
                sb.Append(QuoteIfNeeded(fields[f]));
                if (f < fields.Count - 1)
                {
                    sb.Append(',');
                }
            }
            outLines.Add(sb.ToString());
        }

        var outDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        File.WriteAllLines(outputPath, outLines);
        Console.WriteLine($"Processed {outLines.Count - 1} rows. Output written to {outputPath}");
    }

    /// <summary>
    /// State machine parser that processes an entire CSV file text block.
    /// Safely ignores commas and line breaks that occur inside open quotes.
    /// </summary>
    // AI Assistant Acknowledgement: This file was created or modified with assistance from an AI programming assistant named "GitHub Copilot".
    // Review generated code before use and treat any embedded secrets appropriately.

    private static List<List<string>> ParseFullCsvText(string text)
    {
        var allRecords = new List<List<string>>();
        if (string.IsNullOrEmpty(text)) return allRecords;

        var currentRecord = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                // Handle standard double-quote escaping ("") inside data blocks
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++; // Skip the secondary sequence character token
                }
                else
                {
                    inQuotes = !inQuotes; // Flip state
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // End of field: Add the accumulated field string token to the current record row array
                currentRecord.Add(currentField.ToString());
                currentField.Clear();
            }
            else if (c == '\n' && !inQuotes)
            {
                // End of true row: Flush final field string token, then save full row record array
                currentRecord.Add(currentField.ToString());
                allRecords.Add(currentRecord);

                currentRecord = new List<string>();
                currentField.Clear();
            }
            else if (c == '\r' && !inQuotes)
            {
                // End of true row (Windows Line Endings): Peak ahead to check for \n
                currentRecord.Add(currentField.ToString());
                allRecords.Add(currentRecord);

                currentRecord = new List<string>();
                currentField.Clear();

                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
            }
            else
            {
                // Standard data payload character
                currentField.Append(c);
            }
        }

        // Flush any trailing structural field or line hanging without a clean termination token
        if (currentField.Length > 0 || currentRecord.Count > 0)
        {
            currentRecord.Add(currentField.ToString());
            allRecords.Add(currentRecord);
        }

        return allRecords;
    }
    // Keep your existing CreateFixedBlock, QuoteIfNeeded, and CleanUrl methods completely unchanged below...
    // Create a deterministic 32-byte block from the concatenation of title, website, and username.
    private static byte[] CreateFixedBlock(string title, string website, string username)
    {
        string combined = (title ?? "") + (website ?? "") + (username ?? "");
        var bytes = Encoding.UTF8.GetBytes(combined);
        const int size = 32;
        var block = new byte[size];

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

    /// <summary>
    /// Normalizes a URL to just its protocol and domain (e.g., https://amazon.com).
    /// </summary>
    public static string CleanUrl(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return string.Empty;
        }

        string urlToParse = rawUrl.Trim();
        if (!urlToParse.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !urlToParse.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            urlToParse = "https://" + urlToParse;
        }

        if (Uri.TryCreate(urlToParse, UriKind.Absolute, out Uri? uriResult))
        {
            return $"{uriResult.Scheme}://{uriResult.Host}".ToLowerInvariant();
        }

        return rawUrl.Trim().ToLowerInvariant();
    }
}