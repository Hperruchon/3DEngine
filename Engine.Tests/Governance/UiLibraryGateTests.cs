using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate of ADR-0027 items 1 and 8 (TASK-0084): the library of the user interface
// lives in one host folder. Only the files in 3DEngine/Ui/ name the namespace ImGuiNET,
// and only 3DEngine/3DEngine.csproj pins the package ImGui.NET. A fork or a first-party
// user interface then replaces one folder (ADR-0027 item 9).
//
// The gate reads text, not the semantic model: a name in a comment, in a string or in a
// global using of a project file is an offence too, because each one is a way to reach
// the library from another folder.
public class UiLibraryGateTests
{
    private const string UiFolder = "3DEngine/Ui/";
    private const string PinningProject = "3DEngine/3DEngine.csproj";

    // This file names the namespace in its injected forms, so the scan of the repository
    // leaves it out. The functions that find an offence have no exception.
    private const string ThisGate = "Engine.Tests/Governance/UiLibraryGateTests.cs";

    private static readonly Regex Namespace = new(@"\bImGuiNET\b", RegexOptions.Compiled);

    // MSBuild compares the name of a package with no case (finding T55 of the codebase
    // review of 2026-10-17), so the gate does too.
    private static readonly Regex PackageReference = new(
        @"<PackageReference\b[^>]*\b(?:Include|Update)\s*=\s*""\s*ImGui\.NET\s*""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Fact]
    public void Only_The_Ui_Folder_Of_The_Host_Names_The_Library()
    {
        var offences = NamespaceOffences(SourceFiles().Where(file => file.Path != ThisGate))
            .Concat(PackageOffences(ProjectFiles())).ToArray();

        Assert.True(
            offences.Length == 0,
            "ADR-0027 items 1 and 8: only the files in 3DEngine/Ui/ name the namespace ImGuiNET, and only "
            + "3DEngine/3DEngine.csproj pins the package ImGui.NET. Offences:\n  " + string.Join("\n  ", offences));
    }

    // The forms that the gate must find. Each one is a way to use the library from
    // another folder.
    [Theory]
    [InlineData("3DEngine/NativeThreeDEngine.cs", "using ImGuiNET;\ninternal static class C { }", "names the namespace ImGuiNET")]
    [InlineData("3DEngine.Vulkan/Ui/UiRenderer.cs", "internal static class C { static void F() => ImGuiNET.ImGui.NewFrame(); }", "names the namespace ImGuiNET")]
    [InlineData("3DEngine.ImageTests/PanelTests.cs", "using Gui = ImGuiNET.ImGui;\ninternal static class C { }", "names the namespace ImGuiNET")]
    [InlineData("3DEngine/3DEngine.csproj", "<Project><ItemGroup><Using Include=\"ImGuiNET\" /></ItemGroup></Project>", "names the namespace ImGuiNET")]
    public void A_Use_Of_The_Namespace_Outside_The_Ui_Folder_Is_An_Offence(string path, string text, string expected)
    {
        var offences = NamespaceOffences([(path, text)]);

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    [Theory]
    [InlineData("3DEngine.Vulkan/3DEngine.Vulkan.csproj", "<PackageReference Include=\"ImGui.NET\" Version=\"1.91.6.1\" />")]
    [InlineData("3DEngine.ImageTests/3DEngine.ImageTests.csproj", "<PackageReference Include=\"imgui.net\" Version=\"1.91.6.1\" />")]
    [InlineData("Directory.Build.props", "<PackageReference Update=\"ImGui.NET\" Version=\"1.92.0\" />")]
    public void A_Pin_Of_The_Package_Outside_The_Host_Is_An_Offence(string path, string text)
    {
        var offences = PackageOffences([(path, text)]);

        Assert.True(offences.Any(o => o.StartsWith(path, StringComparison.Ordinal)), "Expected an offence for " + path);
    }

    [Fact]
    public void The_Ui_Folder_And_The_Project_Of_The_Host_Are_Not_Offences()
    {
        Assert.Empty(NamespaceOffences([("3DEngine/Ui/DesktopUi.cs", "using ImGuiNET;\ninternal static class C { }")]));
        Assert.Empty(PackageOffences([(PinningProject, "<PackageReference Include=\"ImGui.NET\" Version=\"1.91.6.1\" />")]));
    }

    private static IEnumerable<string> NamespaceOffences(IEnumerable<(string Path, string Text)> files)
    {
        foreach (var (path, text) in files)
        {
            if (path.StartsWith(UiFolder, StringComparison.Ordinal))
                continue;

            var lines = text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (Namespace.IsMatch(lines[i]))
                    yield return $"{path}:{i + 1} names the namespace ImGuiNET outside {UiFolder}";
            }
        }
    }

    private static IEnumerable<string> PackageOffences(IEnumerable<(string Path, string Text)> files)
    {
        foreach (var (path, text) in files)
        {
            if (path == PinningProject)
                continue;

            if (PackageReference.IsMatch(text))
                yield return $"{path} pins the package ImGui.NET; only {PinningProject} may";
        }
    }

    // Each C# file and each project file of each project, with the folders bin/ and obj/
    // at the root of a project left out, as MSBuild leaves them out.
    private static IEnumerable<(string Path, string Text)> SourceFiles()
        => ProjectDirectories()
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || IsProjectFile(file))
                .Where(file => !InBuildOutput(directory, file)))
            .Select(file => (RepositoryFiles.Relative(file), File.ReadAllText(file)));

    // The project files, and the files that MSBuild imports into each project.
    private static IEnumerable<(string Path, string Text)> ProjectFiles()
        => ProjectDirectories()
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(IsProjectFile)
                .Where(file => !InBuildOutput(directory, file)))
            .Concat(Directory.EnumerateFiles(RepositoryFiles.Root, "Directory.*", SearchOption.TopDirectoryOnly))
            .Distinct()
            .Select(file => (RepositoryFiles.Relative(file), File.ReadAllText(file)));

    private static IEnumerable<string> ProjectDirectories()
        => Directory.EnumerateFiles(RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories)
            .Where(file => !RepositoryFiles.Relative(file).Split('/').Any(part => part is "bin" or "obj" or ".git"))
            .Select(file => Path.GetDirectoryName(file)!)
            .Distinct();

    private static bool IsProjectFile(string file)
        => file.EndsWith(".csproj", StringComparison.Ordinal)
            || file.EndsWith(".props", StringComparison.Ordinal)
            || file.EndsWith(".targets", StringComparison.Ordinal);

    private static bool InBuildOutput(string projectDirectory, string file)
    {
        var relative = Path.GetRelativePath(projectDirectory, file).Replace('\\', '/');
        return relative.StartsWith("bin/", StringComparison.Ordinal) || relative.StartsWith("obj/", StringComparison.Ordinal);
    }
}
