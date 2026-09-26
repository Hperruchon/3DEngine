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

    // The test project holds sample codes in its own tests and is not a source
    // of diagnostics. Each other Engine.* project is. Until TASK-0039 the list
    // named three projects, and Engine.Api.Http and Engine.Geometry.Manifold,
    // which both raise codes, were not read (codebase review finding T2).
    private static readonly string[] ExcludedProjects = ["Engine.Tests"];

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

    public static IEnumerable<string> EngineSourceProjects(string repoRoot)
        => Directory.EnumerateDirectories(repoRoot, "Engine.*")
            .Select(Path.GetFileName)
            .Where(name => name is not null && !ExcludedProjects.Contains(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal);

    public static IEnumerable<string> EnumerateEngineSources(string repoRoot)
    {
        foreach (var project in EngineSourceProjects(repoRoot))
        {
            var projectDir = Path.Combine(repoRoot, project);

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
