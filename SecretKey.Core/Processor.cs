using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SecretKey.Core;

public static class Processor
{
    public static void Process(string inputPath, string outputPath, byte[] monthlyMasterKey, string passwordMask)
    {
        if (!File.Exists(inputPath)) return;

        var lines = File.ReadAllLines(inputPath);
        if (lines.Length == 0) return;

        // Detect if the input CSV contains a Password column that must be sanitized
        var header = ParseCsvLine(lines[0]);
        int passwordIndex = -1;
        for (int h = 0; h < header.Length; h++)
        {
            // Trim whitespace, surrounding quotes, and possible BOM from the first header field
            string col = header[h] ?? string.Empty;
            col = col.Trim();
            col = col.Trim('"', '\'');
            if (h == 0) col = col.TrimStart('\uFEFF');
            if (string.Equals(col, "password", StringComparison.OrdinalIgnoreCase))
            {
                passwordIndex = h;
                break;
            }
        }
        System.Diagnostics.Debug.WriteLine($"Processor: detected header columns=[{string.Join(",", header)}], passwordIndex={passwordIndex}");

        var outLines = new List<string>();
        outLines.Add("Title,Url,Username,Password");

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length < 3) continue;
            string title = parts[0].Trim();
            string url = parts[1].Trim();
            string username = parts[2].Trim();

            // simple normalization
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;

            var block = new byte[32];
            var bytes = Encoding.UTF8.GetBytes((title + url + username));
            Array.Copy(bytes, block, Math.Min(bytes.Length, block.Length));

            var hash = Crypto.HmacSha256(monthlyMasterKey, block);
            var password = Crypto.MapBlobToPattern(hash, passwordMask);

            outLines.Add(JoinCsvLine(new[] { title, url, username, password }));

            // If input had a Password column, blank it out for this row in the sanitized input copy
            if (passwordIndex >= 0)
            {
                var partsList = new System.Collections.Generic.List<string>(parts);
                // Ensure the list has at least as many columns as the header
                while (partsList.Count < header.Length) partsList.Add(string.Empty);
                partsList[passwordIndex] = string.Empty;
                lines[i] = JoinCsvLine(partsList.ToArray());
            }
        }

        // Write output vault
        File.WriteAllLines(outputPath, outLines);

        // If we sanitized the input (blanked a Password column), overwrite the input file in-place
        if (passwordIndex >= 0)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processor: sanitizing input file in-place: {inputPath}");
                File.WriteAllLines(inputPath, lines);
                System.Diagnostics.Debug.WriteLine($"Processor: wrote sanitized input to {inputPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to sanitize input file {inputPath}: {ex}");
            }
        }

// Simple CSV parsing/writing utilities that handle quoted fields and double quotes per RFC4180
static string[] ParseCsvLine(string line)
{
    if (line == null) return Array.Empty<string>();
    var fields = new System.Collections.Generic.List<string>();
    var sb = new System.Text.StringBuilder();
    bool inQuotes = false;
    for (int i = 0; i < line.Length; i++)
    {
        char c = line[i];
        if (inQuotes)
        {
            if (c == '"')
            {
                // If this is a double-quote escape, consume one and append a quote
                if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++; // skip next
                }
                else
                {
                    inQuotes = false; // end of quoted field
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        else
        {
            if (c == ',')
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else
            {
                sb.Append(c);
            }
        }
    }
    fields.Add(sb.ToString());
    return fields.ToArray();
}

static string JoinCsvLine(string[] fields)
{
    if (fields == null) return string.Empty;
    var sb = new System.Text.StringBuilder();
    for (int i = 0; i < fields.Length; i++)
    {
        if (i > 0) sb.Append(',');
        var f = fields[i] ?? string.Empty;
        bool mustQuote = f.Contains(',') || f.Contains('"') || f.Contains('\n') || f.Contains('\r');
        if (mustQuote)
        {
            sb.Append('"');
            sb.Append(f.Replace("\"", "\"\""));
            sb.Append('"');
        }
        else
        {
            sb.Append(f);
        }
    }
    return sb.ToString();
}
    }
}
