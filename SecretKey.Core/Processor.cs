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
        }

        File.WriteAllLines(outputPath, outLines);
    }
}
