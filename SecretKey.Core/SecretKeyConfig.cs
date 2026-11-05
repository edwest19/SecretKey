// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text.Json;

namespace SecretKey.Core;

/// <summary>
/// The settings stored in config.json (see config.example.json and README.md "Configuration").
/// Shared by the command-line app and the desktop app.
/// </summary>
public sealed class SecretKeyConfig
{
    public const string FileName = "config.json";
    public const string DefaultMask = "XxxxxNSxxxNN";

    /// <summary>The 32-byte root key as 64 hexadecimal characters. This is the secret.</summary>
    public string RootKey { get; set; } = string.Empty;

    /// <summary>The input CSV. A relative path is relative to the folder holding config.json.</summary>
    public string InputPath { get; set; } = string.Empty;

    /// <summary>The output CSV. A relative path is relative to the folder holding config.json.</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>The mask for accounts without a passwordmask= note.</summary>
    public string PasswordMask { get; set; } = DefaultMask;

    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>Reads config.json. Throws <see cref="InvalidDataException"/> if it isn't valid settings JSON.</summary>
    public static SecretKeyConfig Load(string path)
    {
        SecretKeyConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<SecretKeyConfig>(File.ReadAllText(path), ReadOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{path} is not valid JSON: {ex.Message}", ex);
        }

        if (config is null)
        {
            throw new InvalidDataException($"{path} is empty.");
        }

        // A JSON null would otherwise leave these null.
        config.RootKey ??= string.Empty;
        config.InputPath ??= string.Empty;
        config.OutputPath ??= string.Empty;
        config.PasswordMask ??= string.Empty;
        return config;
    }

    /// <summary>Writes config.json, indented.</summary>
    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, WriteOptions));
}
