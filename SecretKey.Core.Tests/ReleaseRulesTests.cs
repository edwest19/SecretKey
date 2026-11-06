// ============================================================================
// Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
// AI-GENERATED CODE: This file was written by Claude (Anthropic).
// SecretKey is a project built with AI-generated code; see README.md.
// ============================================================================
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static SecretKey.Core.Tests.TestHelper;

namespace SecretKey.Core.Tests;

/// <summary>
/// Keeps the release settings consistent (README.md "Releases"), so a release isn't
/// discovered to be broken only after a tag is pushed: the MSIX publisher, the version
/// in three places, the changelog, pinned workflow actions, and the solution file.
/// </summary>
[TestClass]
public sealed class ReleaseRulesTests
{
    private static string Read(params string[] path) => File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(path).ToArray()));

    private static XElement ManifestIdentity() =>
        XDocument.Parse(Read("SecretKeyUI", "Package.appxmanifest")).Descendants().Single(e => e.Name.LocalName == "Identity");

    private static string ProjectVersion() =>
        XDocument.Parse(Read("Directory.Build.props")).Descendants("Version").Single().Value;

    [TestMethod]
    public void ManifestPublisher_IsTheNameReleasesAreSignedAs()
    {
        Match signer = Regex.Match(Read(".github", "workflows", "release.yml"), @"EXPECTED_SIGNER:\s*'([^']+)'");
        Assert.IsTrue(signer.Success, "release.yml must set EXPECTED_SIGNER.");

        // Case-sensitive: Windows refuses to sign an MSIX whose Publisher differs from the certificate subject.
        Assert.AreEqual(signer.Groups[1].Value, (string?)ManifestIdentity().Attribute("Publisher"));
    }

    [TestMethod]
    public void Readme_ReleaseSection_MatchesTheWorkflow()
    {
        string workflow = Read(".github", "workflows", "release.yml");
        string readme = Read("README.md");
        string Setting(string name) => Regex.Match(workflow, name + @":\s*'?([^'\r\n]+?)'?\s*$", RegexOptions.Multiline).Groups[1].Value;

        Assert.Contains($"made by `{Setting("EXPECTED_SIGNER")}`", readme);
        Assert.Contains($"(account `{Setting("signing-account-name")}`, profile `{Setting("certificate-profile-name")}`, East US)", readme);
        Assert.Contains("https://eus.codesigning.azure.net/", workflow, "README says East US; the endpoint must be East US's.");
    }

    [TestMethod]
    public void ManifestVersion_IsTheProjectVersionPlusZero()
    {
        string version = ProjectVersion();

        Assert.IsTrue(Regex.IsMatch(version, @"^\d+\.\d+\.\d+$"), $"Directory.Build.props Version '{version}' must be X.Y.Z.");
        Assert.AreEqual(version + ".0", (string?)ManifestIdentity().Attribute("Version"));
    }

    [TestMethod]
    public void Changelog_HasAnEntryForTheProjectVersion()
    {
        string version = ProjectVersion();
        string[] lines = Read("CHANGELOG.md").Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var heading = new Regex(@"^## \[" + Regex.Escape(version) + @"\] - \d{4}-\d{2}-\d{2}\s*$");

        int[] starts = Enumerable.Range(0, lines.Length).Where(i => heading.IsMatch(lines[i])).ToArray();
        Assert.HasCount(1, starts, $"CHANGELOG.md needs exactly one heading '## [{version}] - YYYY-MM-DD'.");

        IEnumerable<string> notes = lines.Skip(starts[0] + 1).TakeWhile(l => !l.StartsWith("## [", StringComparison.Ordinal));
        Assert.IsTrue(notes.Any(l => !string.IsNullOrWhiteSpace(l)), $"The CHANGELOG.md entry for {version} is empty.");
    }

    [TestMethod]
    public void WorkflowActions_ArePinnedToExactCommits()
    {
        var uses = new Regex(@"^\s*(?:-\s*)?uses:\s*(\S+)");
        var pinned = new Regex(@"^[A-Za-z0-9_.-]+/[A-Za-z0-9_./-]+@[0-9a-f]{40}$");
        int checkedCount = 0;

        foreach (string file in Directory.EnumerateFiles(Path.Combine(RepoRoot(), ".github", "workflows"), "*.yml"))
        {
            foreach (string line in File.ReadLines(file))
            {
                Match match = uses.Match(line);
                if (!match.Success)
                {
                    continue;
                }
                checkedCount++;
                Assert.IsTrue(pinned.IsMatch(match.Groups[1].Value),
                    $"{Path.GetFileName(file)}: '{match.Groups[1].Value}' must be pinned to a full commit SHA.");
            }
        }

        Assert.IsGreaterThan(0, checkedCount, "Expected to find actions in the workflows.");
    }

    [TestMethod]
    public void SolutionFile_ListsOnlyFilesThatExist()
    {
        XDocument solution = XDocument.Parse(Read("SecretKey.slnx"));
        string[] paths = solution.Descendants()
            .Where(e => e.Name.LocalName is "File" or "Project")
            .Select(e => (string?)e.Attribute("Path") ?? "")
            .ToArray();

        Assert.IsNotEmpty(paths);
        foreach (string path in paths)
        {
            Assert.IsTrue(File.Exists(Path.Combine(RepoRoot(), path)), $"SecretKey.slnx lists '{path}', which doesn't exist.");
        }
    }
}
