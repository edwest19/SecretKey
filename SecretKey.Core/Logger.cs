using System;
using System.IO;
using System.Text;

namespace SecretKey.Core;

internal static class Logger
{
    private static readonly object _sync = new object();

    // Append a minimal, non-sensitive event to a workspace-local log file.
    public static void LogSanitization(string workspaceDirectory, string outputPath, int rowsProcessed)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(workspaceDirectory)) workspaceDirectory = AppContext.BaseDirectory;
            string logPath = Path.Combine(workspaceDirectory, "secretkey_events.log");

            var sb = new StringBuilder();
            sb.Append(DateTime.UtcNow.ToString("o")); // ISO 8601 UTC
            sb.Append(" \t");
            sb.Append("ACTION=SanitizedInput");
            sb.Append(" \t");
            sb.Append($"Output={Path.GetFileName(outputPath)}");
            sb.Append(" \t");
            sb.Append($"Rows={rowsProcessed}");
            sb.AppendLine();

            lock (_sync)
            {
                File.AppendAllText(logPath, sb.ToString());
            }
        }
        catch
        {
            // Best-effort logging only; swallow any exceptions to avoid breaking processing
        }
    }
}
