// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text;

namespace SecretKey.Core;

/// <summary>One CSV record (row) and the file line it starts on, for error messages.</summary>
public sealed record CsvRecord(int Line, List<string> Fields);

/// <summary>
/// Reads and writes CSV as described in RFC 4180: comma-separated fields, double-quoted fields
/// may contain commas, line breaks and doubled quotes (""). Spaces before an opening quote
/// and after a closing quote are ignored. Field values are otherwise kept exactly as written.
/// </summary>
public static class Csv
{
    /// <summary>Parses CSV text into records. Throws <see cref="InvalidDataException"/> for malformed quoting.</summary>
    public static List<CsvRecord> Parse(string text)
    {
        var records = new List<CsvRecord>();
        var fields = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        bool afterClosingQuote = false;
        bool fieldWasQuoted = false;
        int line = 1;
        int recordLine = 1;
        int quoteLine = 1;

        void EndField()
        {
            fields.Add(field.ToString());
            field.Clear();
            afterClosingQuote = false;
            fieldWasQuoted = false;
        }

        void EndRecord()
        {
            EndField();
            records.Add(new CsvRecord(recordLine, fields));
            fields = new List<string>();
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                        afterClosingQuote = true;
                    }
                }
                else
                {
                    if (c == '\n' || (c == '\r' && !(i + 1 < text.Length && text[i + 1] == '\n')))
                    {
                        line++;
                    }
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"' when afterClosingQuote:
                    throw new InvalidDataException($"Line {line}: unexpected quote after a quoted field.");

                case '"' when string.IsNullOrWhiteSpace(field.ToString()):
                    field.Clear(); // spaces before an opening quote are ignored
                    inQuotes = true;
                    fieldWasQuoted = true;
                    quoteLine = line;
                    break;

                case ',':
                    EndField();
                    break;

                case '\r':
                case '\n':
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                    EndRecord();
                    line++;
                    recordLine = line;
                    break;

                default:
                    if (afterClosingQuote)
                    {
                        if (c == ' ' || c == '\t')
                        {
                            break; // spaces after a closing quote are ignored
                        }
                        throw new InvalidDataException($"Line {line}: unexpected text after a quoted field.");
                    }
                    field.Append(c); // includes a quote in the middle of an unquoted field, kept as-is
                    break;
            }
        }

        if (inQuotes)
        {
            throw new InvalidDataException($"Line {quoteLine}: a quoted field is never closed.");
        }

        if (fields.Count > 0 || field.Length > 0 || fieldWasQuoted)
        {
            EndRecord();
        }

        return records;
    }

    /// <summary>Formats one record, quoting a field only when it needs it.</summary>
    public static string FormatRecord(IEnumerable<string> fields) =>
        string.Join(",", fields.Select(FormatField));

    /// <summary>Quotes a field that contains a comma, quote or line break, or starts or ends with whitespace.</summary>
    public static string FormatField(string? value)
    {
        string v = value ?? string.Empty;
        bool needsQuotes = v.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
            || (v.Length > 0 && (char.IsWhiteSpace(v[0]) || char.IsWhiteSpace(v[^1])));
        return needsQuotes ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }
}
