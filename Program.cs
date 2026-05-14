using System;
using System.IO;
using System.Text.Json;

internal partial class Program
{
    static void Main(string[] args)
    {
        string folder = AppDomain.CurrentDomain.BaseDirectory;
        string configPath = Path.Combine(folder, "config.json");

        Console.WriteLine("======================================");
        Console.WriteLine("   NEW CODE LOADED SUCCESSFULLY       ");
        Console.WriteLine("======================================");
        
        if (File.Exists(configPath))
        {
            // Read the file content
            string jsonString = File.ReadAllText(configPath);

            // Deserialize into a dynamic object or a specific class
            var config = JsonSerializer.Deserialize<ConfigData>(jsonString);

            // Print the results
            Console.WriteLine("--- Secret Key Configuration ---");
            Console.WriteLine($"Input Path:  {config?.InputPath}");
            Console.WriteLine($"Output Path: {config?.OutputPath}");
            string rootPreview = string.IsNullOrEmpty(config?.RootKey) ? "(not set)" : (config.RootKey.Length <= 4 ? "****" : config.RootKey.Substring(0, 2) + new string('*', Math.Min(4, config.RootKey.Length - 2)));
            Console.WriteLine($"Root Key:    {rootPreview}");
            Console.WriteLine("--------------------------------");

            // Validate args: expect DateCode as args[0] in YYMM
            if (args.Length == 0)
            {
                Console.WriteLine("Error: DateCode argument required (YYMM). Example: 2605");
                return;
            }

            string dateCode = args[0];
            if (dateCode.Length != 4 || !int.TryParse(dateCode, out _))
            {
                Console.WriteLine("Error: DateCode must be a 4-digit YYMM string.");
                return;
            }

            // Derive Monthly Master Key
            var masterKey = Crypto.DeriveMonthlyMasterKey(config?.RootKey ?? string.Empty, dateCode);

            // Run processor
            Processor.Process(config?.InputPath ?? string.Empty, config?.OutputPath ?? string.Empty, masterKey);
        }
        else
        {
            Console.WriteLine("Error: config.json not found.");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}

// A simple class to hold your paths
public class ConfigData
{
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string RootKey { get; set; } = string.Empty;
}