using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate for two documents that an agent reads first. Register entry R-0019
// recorded six statements that described a repository state that no longer
// existed, and its exit asked for a gate that fails when docs/INDEX.md names a
// path that does not exist. The rules review of 2026-09-25 found fourteen such
// statements (findings F26 to F32) and a hand-maintained gate table that said
// "two gates exist" while thirteen did.
//
// The gate reads two things. Each path in docs/INDEX.md must exist. Each test
// class whose name ends in GateTests must have a row in docs/templates.md,
// section "What each gate reads".
public class DocumentPathGateTests
{
    // A backticked token that looks like a path: it contains a separator, or
    // it ends with a known extension. A token with a space, such as
    // `POST /commands`, and a token that starts with a slash, such as a route,
    // are not paths in the repository.
    private static readonly Regex BacktickedToken = new(@"`([^`\s]+)`", RegexOptions.Compiled);

    private static readonly string[] PathExtensions =
        [".md", ".yml", ".json", ".csproj", ".txt", ".config", ".sln", ".cs"];

    // A markdown link target that is not a URL and not an anchor. The target
    // is relative to the directory of the document.
    private static readonly Regex LinkTarget = new(@"\]\(([^)#\s]+)\)", RegexOptions.Compiled);

    private static readonly Regex GateClass = new(@"^(\w+GateTests)\.cs$", RegexOptions.Compiled);

    [Fact]
    public void Each_Path_In_The_Index_Exists()
    {
        var index = RepositoryFiles.Read("docs", "INDEX.md");
        var problems = new List<string>();
        var lines = index.Replace("\r", string.Empty).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            foreach (Match match in BacktickedToken.Matches(lines[i]))
            {
                var token = match.Groups[1].Value;
                if (token.StartsWith('/') || !LooksLikeAPath(token))
                    continue;

                if (!Exists(RepositoryFiles.Path(Trim(token).Split('/'))))
                    problems.Add($"docs/INDEX.md:{i + 1} names `{token}`, which does not exist");
            }

            foreach (Match match in LinkTarget.Matches(lines[i]))
            {
                var target = match.Groups[1].Value;
                if (target.StartsWith("http", StringComparison.Ordinal))
                    continue;

                if (!Exists(RepositoryFiles.Path(new[] { "docs" }.Concat(Trim(target).Split('/')).ToArray())))
                    problems.Add($"docs/INDEX.md:{i + 1} links to `{target}`, which does not exist");
            }
        }

        Assert.True(
            problems.Count == 0,
            "docs/INDEX.md is the map. A path on the map must exist. Correct the row or remove it.\n  "
            + string.Join("\n  ", problems));
    }

    [Fact]
    public void Each_Gate_Class_Has_A_Row_In_The_Gate_Table()
    {
        var templates = RepositoryFiles.Read("docs", "templates.md").Replace("\r", string.Empty);
        var section = Section(templates, "What each gate reads");
        Assert.True(section is not null, "docs/templates.md has no section \"What each gate reads\".");

        var missing = Directory
            .EnumerateFiles(RepositoryFiles.Path("Engine.Tests"), "*GateTests.cs", SearchOption.AllDirectories)
            .Select(RepositoryFiles.Relative)
            .Where(relative => !relative.Contains("/obj/", StringComparison.Ordinal) && !relative.Contains("/bin/", StringComparison.Ordinal))
            .Select(relative => GateClass.Match(System.IO.Path.GetFileName(relative)).Groups[1].Value)
            .Where(name => name.Length > 0 && !section!.Contains($"`{name}`", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Each gate class has one row in docs/templates.md, section \"What each gate reads\", so "
            + "that the table and the gates agree. Add a row for:\n  " + string.Join("\n  ", missing));
    }

    private static bool LooksLikeAPath(string token)
        => token.Contains('/') || PathExtensions.Any(extension => token.EndsWith(extension, StringComparison.Ordinal));

    private static string Trim(string token)
        => token.TrimEnd('*').TrimEnd('/');

    private static bool Exists(string absolute)
        => File.Exists(absolute) || Directory.Exists(absolute);

    // The text from the level-2 heading that ends with the given words to the
    // next level-2 heading, or null.
    private static string? Section(string text, string headingEnd)
    {
        var lines = text.Split('\n');
        var start = Array.FindIndex(lines, l => l.StartsWith("## ", StringComparison.Ordinal) && l.EndsWith(headingEnd, StringComparison.Ordinal));
        if (start < 0)
            return null;

        var end = Array.FindIndex(lines, start + 1, l => l.StartsWith("## ", StringComparison.Ordinal));
        return string.Join("\n", lines[(start + 1)..(end < 0 ? lines.Length : end)]);
    }
}
