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
        var header = lines[0].Split(',');
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

        var outLines = new List<string>();
        outLines.Add("Title,Url,Username,Password");

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
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

            outLines.Add($"{title},{url},{username},{password}");

            // If input had a Password column, blank it out for this row in the sanitized input copy
            if (passwordIndex >= 0)
            {
                var partsList = new System.Collections.Generic.List<string>(parts);
                // Ensure the list has at least as many columns as the header
                while (partsList.Count < header.Length) partsList.Add(string.Empty);
                partsList[passwordIndex] = string.Empty;
                lines[i] = string.Join(',', partsList);
            }
        }

        // Write output vault
        File.WriteAllLines(outputPath, outLines);

        // If we sanitized the input (blanked a Password column), overwrite the input file with a backup
        if (passwordIndex >= 0)
        {
            try
            {
                // create a timestamped backup of the original input
                string backupPath = inputPath + ".bak." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                File.Copy(inputPath, backupPath, overwrite: true);
                File.WriteAllLines(inputPath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to sanitize input file {inputPath}: {ex}");
            }
        }
    }
}
