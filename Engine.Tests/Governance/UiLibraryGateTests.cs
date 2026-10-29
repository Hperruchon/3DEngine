using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    private static readonly Regex MsBuildEscape = new("%([0-9A-Fa-f]{2})", RegexOptions.Compiled);

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

    // The forms of finding T64 of the codebase review of 2026-10-28 (TASK-0089); each one
    // compiled and passed the gate of main.
    [Theory]
    [InlineData("3DEngine/NativeThreeDEngine.cs", "using ImGu\\u0069NET;\ninternal static class C { }", "with an escape")]
    [InlineData("3DEngine/NativeThreeDEngine.cs", "internal static class C { [System.Runtime.InteropServices.DllImport(\"cimgui\")] static extern void igNewFrame(); }", "native library cimgui")]
    [InlineData("3DEngine/NativeThreeDEngine.cs", "internal static class C { static nint L() => System.Runtime.InteropServices.NativeLibrary.Load(\"CIMGUI.dll\"); }", "native library cimgui")]
    public void Each_Form_Of_Finding_T64_In_A_Source_Is_An_Offence(string path, string text, string expected)
    {
        var offences = NamespaceOffences([(path, text)]);

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    [Theory]
    [InlineData("3DEngine.Vulkan/3DEngine.Vulkan.csproj", "<Project><ItemGroup><PackageReference Include='ImGui.NET' Version='1.91.6.1' /></ItemGroup></Project>")]
    [InlineData("3DEngine.Vulkan/ui-packages.xml", "<Project><ItemGroup><packagereference update=\" imgui.net \" /></ItemGroup></Project>")]
    public void Each_Form_Of_Finding_T64_In_A_Project_File_Is_An_Offence(string path, string text)
    {
        Assert.Contains(PackageOffences([(path, text)]), offence => offence.StartsWith(path, StringComparison.Ordinal));
    }

    // The forms of finding T70 of the second codebase review of 2026-10-28 (TASK-0098): a
    // global using in 3DEngine/Ui/ opens the namespace to each file of the project, and a
    // constant that the compiler concatenates hides the name of the native library. Each one
    // compiled and passed the gate of main.
    [Theory]
    [InlineData("3DEngine/Ui/Usings.cs", "global using ImGuiNET;", "a global using")]
    [InlineData("3DEngine/Ui/Usings.cs", "global using Gui = ImGuiNET.ImGui;", "a global using")]
    [InlineData("3DEngine/Ui/Usings.cs", "global using static ImGu\\u0069NET.ImGui;", "a global using")]
    [InlineData("3DEngine/NativeThreeDEngine.cs", "internal static class C { [System.Runtime.InteropServices.DllImport(\"cim\" + \"gui\")] static extern void igNewFrame(); }", "native library cimgui")]
    [InlineData("3DEngine/NativeThreeDEngine.cs", "internal static class C { const string Part = \"cim\"; const string Name = Part + \"GUI\"; [System.Runtime.InteropServices.DllImport(Name)] static extern void igNewFrame(); }", "native library cimgui")]
    [InlineData("3DEngine/NativeThreeDEngine.cs", "internal static class C { static nint L() => System.Runtime.InteropServices.NativeLibrary.Load($\"cim{\"gui\"}\"); }", "native library cimgui")]
    public void Each_Form_Of_Finding_T70_Is_An_Offence(string path, string text, string expected)
    {
        var offences = NamespaceOffences([(path, text)]);

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    // The forms of finding T82 of the codebase review of 2026-10-29 (TASK-0107): a raw string,
    // nameof, an escape in a string and a constant of another file. Each one compiled and
    // passed the gate of main.
    [Theory]
    [InlineData("internal static class C { [System.Runtime.InteropServices.DllImport(\"\"\"cimgui\"\"\")] static extern void igNewFrame(); }")]
    [InlineData("internal static class cimgui { [System.Runtime.InteropServices.DllImport(nameof(cimgui))] static extern void igNewFrame(); }")]
    [InlineData("internal static class C { [System.Runtime.InteropServices.DllImport(\"cim\\u0067ui\")] static extern void igNewFrame(); }")]
    [InlineData("internal static class C { [System.Runtime.InteropServices.DllImport(Names.Part + \"gui\")] static extern void igNewFrame(); }")]
    public void Each_Form_Of_Finding_T82_Is_An_Offence(string text)
    {
        var offences = NamespaceOffences([("3DEngine/NativeThreeDEngine.cs", text), ("3DEngine/Names.cs", "internal static class Names { internal const string Part = \"cim\"; }")]).ToList();

        Assert.True(
            offences.Any(o => o.StartsWith("3DEngine/NativeThreeDEngine.cs:1 names the native library cimgui", StringComparison.Ordinal)),
            "Expected an offence in 3DEngine/NativeThreeDEngine.cs. Offences:\n  " + string.Join("\n  ", offences));
    }

    // Finding T71 (TASK-0098): MSBuild unescapes %2E to a point, so NuGet restored ImGui.NET
    // for a name that the gate did not read as ImGui.NET.
    [Theory]
    [InlineData("3DEngine.Vulkan/3DEngine.Vulkan.csproj", "<Project><ItemGroup><PackageReference Include=\"ImGui%2ENET\" Version=\"1.91.6.1\" /></ItemGroup></Project>")]
    [InlineData("3DEngine.Vulkan/3DEngine.Vulkan.csproj", "<PackageReference Include=\"%49mGui.NET\" Version=\"1.91.6.1\" />")]
    public void Each_Form_Of_Finding_T71_Is_An_Offence(string path, string text)
    {
        Assert.Contains(PackageOffences([(path, text)]), offence => offence.StartsWith(path, StringComparison.Ordinal));
    }

    [Fact]
    public void The_Ui_Folder_And_The_Project_Of_The_Host_Are_Not_Offences()
    {
        Assert.Empty(NamespaceOffences([("3DEngine/Ui/DesktopUi.cs", "using ImGuiNET;\ninternal static class C { }")]));
        Assert.Empty(PackageOffences([(PinningProject, "<PackageReference Include=\"ImGui.NET\" Version=\"1.91.6.1\" />")]));
    }

    private static IEnumerable<string> NamespaceOffences(IEnumerable<(string Path, string Text)> files)
    {
        var list = files.ToList();
        foreach (var (path, text) in list)
        {
            var inUiFolder = path.StartsWith(UiFolder, StringComparison.Ordinal);
            if (!inUiFolder)
            {
                var lines = text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    if (Namespace.IsMatch(lines[i]))
                        yield return $"{path}:{i + 1} names the namespace ImGuiNET outside {UiFolder}";
                }
            }

            if (!path.EndsWith(".cs", StringComparison.Ordinal))
                continue;

            var root = CSharpSyntaxTree.ParseText(text).GetRoot();
            static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            // Finding T70 of the second codebase review of 2026-10-28 (TASK-0098): a global
            // using applies to each file of the project, so it is an offence in 3DEngine/Ui/ too.
            foreach (var directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                if (directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
                    && directive.DescendantTokens().Any(token => token.ValueText == "ImGuiNET"))
                    yield return $"{path}:{Line(directive)} opens the namespace ImGuiNET to each file of the project with a global using";
            }

            if (inUiFolder)
                continue;

            // Finding T64 of the codebase review of 2026-10-28 (TASK-0089): the text misses
            // an escaped name. The tokens of the compiler give the value of each name with each
            // escape resolved.
            foreach (var token in root.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "ImGuiNET" && token.Text != "ImGuiNET")
                    yield return $"{path}:{Line(token.Parent!)} names the namespace ImGuiNET with an escape outside {UiFolder}";
            }
        }

        // Findings T64, T70 and T82 (TASK-0089, TASK-0098, TASK-0107): a string literal, a join,
        // an interpolation of constants, a raw string, nameof and a constant of another file
        // each give the name of the native library. Until TASK-0107 the gate read a string
        // literal token and computed the joins of one file itself, and [DllImport("""cimgui""")]
        // and [DllImport(nameof(cimgui))] passed. The compiler now gives the value of each one.
        var sources = list.Where(file => file.Path.EndsWith(".cs", StringComparison.Ordinal));
        foreach (var (path, line, value) in ConstantStrings.Values(sources).Distinct())
        {
            if (!path.StartsWith(UiFolder, StringComparison.Ordinal) && value.Contains("cimgui", StringComparison.OrdinalIgnoreCase))
                yield return $"{path}:{line} names the native library cimgui outside {UiFolder}";
        }
    }

    private static IEnumerable<string> PackageOffences(IEnumerable<(string Path, string Text)> files)
    {
        foreach (var (path, text) in files)
        {
            if (path == PinningProject)
                continue;

            // Finding T71 (TASK-0098): MSBuild reads %XX as the character of that code, so the
            // gate reads the text as MSBuild does.
            var unescaped = MsBuildEscape.Replace(text, match => ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString());
            if (PackageReference.IsMatch(unescaped) || PinsByXml(unescaped))
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
            // Finding T64: a file that a project imports with another extension, for example
            // ui-packages.xml, is a file that MSBuild reads too.
            .Concat(Directory.EnumerateFiles(RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories)
                .Where(file => !RepositoryFiles.Relative(file).Split('/').Any(part => part is "bin" or "obj" or ".git" or ".claude"))
                .SelectMany(DependencyDirectionGateTests.FilesThatMsBuildReads))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(file => (RepositoryFiles.Relative(file), File.ReadAllText(file)));

    // A worktree of another session under .claude/ is a copy of an older commit, not a project
    // of this checkout (TASK-0098, item 10), as DependencyDirectionGateTests says.
    private static IEnumerable<string> ProjectDirectories()
        => Directory.EnumerateFiles(RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories)
            .Where(file => !RepositoryFiles.Relative(file).Split('/').Any(part => part is "bin" or "obj" or ".git" or ".claude"))
            .Select(file => Path.GetDirectoryName(file)!)
            .Distinct();

    // Finding T64: an attribute in single quotes passed the text of PackageReference. The XML
    // reader reads each quote, and names with no regard to case, as MSBuild does.
    private static bool PinsByXml(string text)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(text);
        }
        catch (System.Xml.XmlException)
        {
            return false;
        }

        return document.Descendants()
            .Where(element => DependencyDirectionGateTests.Is(element, "PackageReference"))
            .Any(element => new[] { "Include", "Update" }
                .Select(attribute => DependencyDirectionGateTests.Value(element, attribute))
                .Any(value => value is not null && value.Split(';').Any(part => string.Equals(part.Trim(), "ImGui.NET", StringComparison.OrdinalIgnoreCase))));
    }

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
