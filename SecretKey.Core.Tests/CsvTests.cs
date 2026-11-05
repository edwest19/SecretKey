// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
using SecretKey.Core;

namespace SecretKey.Core.Tests;

/// <summary>Tests for the CSV reader and writer (README.md "Input file").</summary>
[TestClass]
public sealed class CsvTests
{
    private static string[][] Fields(string text) =>
        Csv.Parse(text).Select(r => r.Fields.ToArray()).ToArray();

    [TestMethod]
    public void SimpleRows_AnyLineEnding()
    {
        foreach (string newline in new[] { "\r\n", "\n", "\r" })
        {
            string[][] rows = Fields("a,b,c" + newline + "1,2,3" + newline);
            Assert.HasCount(2, rows);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, rows[1]);
        }
    }

    [TestMethod]
    public void LastLineWithoutNewline_AndEmptyLastField()
    {
        string[][] rows = Fields("a,b\n1,");
        CollectionAssert.AreEqual(new[] { "1", "" }, rows[1]);
    }

    [TestMethod]
    public void QuotedFields_KeepCommasQuotesAndLineBreaks()
    {
        string[][] rows = Fields("Title,Notes\r\n\"Smith, John\",\"line one\r\nsays \"\"hi\"\"\nline three\"\r\nNext,row\r\n");

        CollectionAssert.AreEqual(new[] { "Smith, John", "line one\r\nsays \"hi\"\nline three" }, rows[1]);
        CollectionAssert.AreEqual(new[] { "Next", "row" }, rows[2]);
    }

    [TestMethod]
    public void RecordLineNumbers_CountLineBreaksInsideQuotes()
    {
        List<CsvRecord> records = Csv.Parse("h1,h2\n\"a\nb\nc\",x\nlast,row\n");

        CollectionAssert.AreEqual(new[] { 1, 2, 5 }, records.Select(r => r.Line).ToArray());
    }

    [TestMethod]
    public void SpacesAroundQuotes_AreIgnored_AndUnquotedSpacesAreKept()
    {
        string[][] rows = Fields("  \"quoted\"  ,  plain  ");
        CollectionAssert.AreEqual(new[] { "quoted", "  plain  " }, rows[0]);
    }

    [TestMethod]
    public void QuoteInsideAnUnquotedField_IsKept()
    {
        CollectionAssert.AreEqual(new[] { "27\" monitor", "x" }, Fields("27\" monitor,x")[0]);
    }

    [TestMethod]
    public void UnclosedQuote_IsRejectedWithItsLine()
    {
        var ex = Assert.ThrowsExactly<InvalidDataException>(() => Csv.Parse("a,b\n1,\"never closed\n2,3\n"));
        Assert.Contains("Line 2", ex.Message);
    }

    [TestMethod]
    public void TextAfterClosingQuote_IsRejected()
    {
        var ex = Assert.ThrowsExactly<InvalidDataException>(() => Csv.Parse("a,b\n\"x\"y,1\n"));
        Assert.Contains("Line 2", ex.Message);
    }

    [TestMethod]
    [DataRow("plain", "plain")]
    [DataRow("a,b", "\"a,b\"")]
    [DataRow("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [DataRow("two\nlines", "\"two\nlines\"")]
    [DataRow(" padded ", "\" padded \"")]
    [DataRow("", "")]
    public void FormatField_QuotesOnlyWhenNeeded(string value, string expected)
    {
        Assert.AreEqual(expected, Csv.FormatField(value));
    }

    [TestMethod]
    public void FormatThenParse_GivesBackTheSameValues()
    {
        string[] values = { "Smith, John", "say \"hi\"", "multi\r\nline", " padded ", "", "plain" };

        CollectionAssert.AreEqual(values, Fields(Csv.FormatRecord(values) + "\r\n")[0]);
    }
}
