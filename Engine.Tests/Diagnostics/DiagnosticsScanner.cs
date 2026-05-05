using System.Text.RegularExpressions;

namespace Engine.Tests.Diagnostics;

internal static class DiagnosticsScanner
{
    // <severity>-<subsystem>-<short-tag> per docs/diagnostics.md.
    // Tag is 2+ chars to keep the shape descriptive; severity must be E/W/I.
    public static readonly Regex CodePattern = new(
        @"\b[EWI]-[A-Z][A-Z0-9]*-[A-Z][A-Z0-9-]*[A-Z0-9]\b",
        RegexOptions.Compiled);

    private static readonly Regex BacktickedCodePattern = new(
        @"`(?<code>[EWI]-[A-Z][A-Z0-9]*-[A-Z][A-Z0-9-]*[A-Z0-9])`",
        RegexOptions.Compiled);

    public static readonly string[] EngineSourceProjects =
    {
        "Engine.Contracts",
        "Engine.Core",
        "Engine.Cli",
    };

    public static string FindRepoRoot(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);
        for (var depth = 0; depth < 10 && current is not null; depth++)
        {
            if (File.Exists(Path.Combine(current.FullName, "3DEngine.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate 3DEngine.sln above '{startDirectory}'.");
    }

    public static IEnumerable<string> ExtractCodes(string text)
    {
        foreach (Match match in CodePattern.Matches(text))
            yield return match.Value;
    }

    public static HashSet<string> ParseRegistry(string registryMarkdown)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in BacktickedCodePattern.Matches(registryMarkdown))
            codes.Add(match.Groups["code"].Value);
        return codes;
    }

    public static IEnumerable<string> EnumerateEngineSources(string repoRoot)
    {
        foreach (var project in EngineSourceProjects)
        {
            var projectDir = Path.Combine(repoRoot, project);
            if (!Directory.Exists(projectDir))
                continue;

            foreach (var file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
            {
                if (HasExcludedSegment(repoRoot, file))
                    continue;
                yield return file;
            }
        }
    }

    private static bool HasExcludedSegment(string repoRoot, string filePath)
    {
        var relative = Path.GetRelativePath(repoRoot, filePath);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var segment in segments)
        {
            if (string.Equals(segment, "bin", StringComparison.Ordinal) ||
                string.Equals(segment, "obj", StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
