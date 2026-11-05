// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic), replacing an
// earlier version created with GitHub Copilot. SecretKey is a project built with
// AI-generated code; see README.md.
// ============================================================================
using SecretKey.Core;

/// <summary>
/// Command-line app (README.md "Command-line app"):
///   SecretKey &lt;YYMM&gt; [workspace folder]
/// Reads config.json from the workspace folder (default: the folder this program is in).
/// Exit code 0 on success, 1 on any error.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        int exitCode = Run(args);

        // Only pause if a real user is interacting with a standard console window.
        if (!Console.IsInputRedirected)
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        return exitCode;
    }

    private static int Run(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.WriteLine("Usage: SecretKey <YYMM> [workspace folder]");
            Console.WriteLine("Example: SecretKey 2611 \"C:\\Users\\me\\Documents\\secretkey\"");
            return 1;
        }

        string dateCode = args[0];
        string workspace = Path.GetFullPath(args.Length == 2 ? args[1] : AppContext.BaseDirectory);
        string configPath = Path.Combine(workspace, SecretKeyConfig.FileName);

        try
        {
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Error: {configPath} not found.");
                return 1;
            }

            SecretKeyConfig config = SecretKeyConfig.Load(configPath);

            Console.WriteLine($"Date code:     {dateCode}");
            Console.WriteLine($"Workspace:     {workspace}");
            Console.WriteLine($"Input file:    {config.InputPath}");
            Console.WriteLine($"Output file:   {config.OutputPath}");
            Console.WriteLine($"Password mask: {config.PasswordMask}");
            Console.WriteLine($"Root key:      {(string.IsNullOrWhiteSpace(config.RootKey) ? "(not set)" : "(set, not shown)")}");

            ProcessResult result = Processor.Run(config, workspace, dateCode);

            Console.WriteLine($"Done: {result.Accounts} passwords written to {config.OutputPath}" +
                (result.CustomMasks > 0 ? $" ({result.CustomMasks} with their own passwordmask=)." : "."));
            if (result.InputPasswordsCleared)
            {
                Console.WriteLine("Old passwords were cleared from the input file.");
            }
            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
