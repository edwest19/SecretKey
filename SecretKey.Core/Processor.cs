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

        var header = ParseCsvLine(lines[0]);
        int passwordIndex = -1;
        for (int h = 0; h < header.Count; h++)
        {
            string col = header[h] ?? string.Empty;
            if (h == 0) col = col.TrimStart('\uFEFF');
            col = col.Trim();
            col = col.Trim('"', '\'');
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
            if (parts.Count < 3) continue;
            string title = parts.Count > 0 ? parts[0].Trim() : string.Empty;
            string url = parts.Count > 1 ? parts[1].Trim() : string.Empty;
            string username = parts.Count > 2 ? parts[2].Trim() : string.Empty;

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(url)) url = "https://" + url;

            var block = new byte[32];
            var bytes = Encoding.UTF8.GetBytes((title + url + username));
            Array.Copy(bytes, block, Math.Min(bytes.Length, block.Length));

            var hash = Crypto.HmacSha256(monthlyMasterKey, block);
            var password = Crypto.MapBlobToPattern(hash, passwordMask);

            outLines.Add(JoinCsvLine(new[] { title, url, username, password }));

            if (passwordIndex >= 0)
            {
                var partsList = new List<string>(parts);
                while (partsList.Count < header.Count) partsList.Add(string.Empty);
                partsList[passwordIndex] = string.Empty;
                lines[i] = JoinCsvLine(partsList);
            }
        }

        File.WriteAllLines(outputPath, outLines);

        if (passwordIndex >= 0)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processor: sanitizing input file in-place: {inputPath}");
                File.WriteAllLines(inputPath, lines);
                System.Diagnostics.Debug.WriteLine($"Processor: wrote sanitized input to {inputPath}");
                try
                {
                    var workspaceDir = Path.GetDirectoryName(inputPath) ?? AppContext.BaseDirectory;
                    Logger.LogSanitization(workspaceDir, outputPath, Math.Max(0, outLines.Count - 1));
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to sanitize input file {inputPath}: {ex}");
            }
        }
    }

    // Minimal RFC-style CSV parser for single-line records (handles quoted fields and escaped quotes)
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        if (line == null) return fields;

        int i = 0;
        int len = line.Length;
        while (i < len)
        {
            if (line[i] == '\"')
            {
                i++; // skip opening quote
                var sb = new StringBuilder();
                while (i < len)
                {
                    if (line[i] == '\"')
                    {
                        if (i + 1 < len && line[i + 1] == '\"')
                        {
                            sb.Append('\"');
                            i += 2; // escaped quote
                            continue;
                        }
                        else
                        {
                            i++; // closing quote
                            break;
                        }
                    }
                    sb.Append(line[i]);
                    i++;
                }
                // skip optional spaces after closing quote
                while (i < len && line[i] != ',') i = (line[i] == ' ') ? i + 1 : i;
                if (i < len && line[i] == ',') i++; // skip comma
                fields.Add(sb.ToString());
            }
            else
            {
                var sb = new StringBuilder();
                while (i < len && line[i] != ',')
                {
                    sb.Append(line[i]);
                    i++;
                }
                if (i < len && line[i] == ',') i++; // skip comma
                fields.Add(sb.ToString().Trim());
            }
        }

        // Handle trailing empty field (line ending with comma)
        if (line.EndsWith(",")) fields.Add(string.Empty);

        return fields;
    }

    private static string JoinCsvLine(IEnumerable<string> fields)
    {
        var first = true;
        var sb = new StringBuilder();
        foreach (var f in fields)
        {
            if (!first) sb.Append(',');
            first = false;
            var val = f ?? string.Empty;
            bool needsQuote = val.Contains(',') || val.Contains('\"') || val.Contains('\n') || val.Contains('\r') || val.StartsWith(" ") || val.EndsWith(" ");
            if (needsQuote)
            {
                sb.Append('\"');
                sb.Append(val.Replace("\"", "\"\""));
                sb.Append('\"');
            }
            else sb.Append(val);
        }
        return sb.ToString();
    }
}