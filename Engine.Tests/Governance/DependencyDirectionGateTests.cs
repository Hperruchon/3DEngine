using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Engine.Tests.Governance;

// The gate that CLAUDE.md has listed for months with no check behind it.
// Register entry R-0004 recorded that gap. The working agreement of 2026-09-20
// called such a rule a wish rather than a rule.
//
// The gate reads each project file in the working tree as XML. It needs no
// build and no reflection, therefore it also catches a reference that compiles.
// Until TASK-0043 it read each file with one regular expression, which kept the
// last path of an Include with two paths and needed one attribute order; a
// reference from Engine.Core to 3DEngine.Core passed that way (codebase review
// of 2026-09-30, finding T4).
public class DependencyDirectionGateTests
{
    // The enumeration must skip a git worktree. The repository held three of
    // them under .claude/worktrees/, and each one was a full copy pinned to an
    // older commit. Without this filter a stale copy shadows the real project
    // file, and the gate then reports a rule as satisfied when it is broken.
    // An injection test found this defect.
    //
    // Finding T68 of the second codebase review of 2026-10-28 (TASK-0097, TASK-0098): MSBuild
    // leaves out bin/ and obj/ at the root of a project only, so the gate leaves out a folder
    // bin or obj only when the folder above it holds a project file.
    internal static bool IsOutOfScope(string relative, Func<string, bool> holdsProject)
    {
        if (relative.StartsWith(".claude/", StringComparison.Ordinal))
            return true;

        var parts = relative.Split('/');
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] is "bin" or "obj" && holdsProject(string.Join('/', parts[..i])))
                return true;
        }
        return false;
    }

    private static bool HoldsProject(string relativeDirectory)
        => Directory.EnumerateFiles(Path.Combine(RepositoryFiles.Root, relativeDirectory), "*.csproj").Any();

    internal static List<string> ProjectFiles()
        => Directory
            .EnumerateFiles(RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !IsOutOfScope(RepositoryFiles.Relative(f), HoldsProject))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

    [Theory]
    [InlineData("Engine.Tests/bin/Debug/net10.0/Probe.csproj", true)]
    [InlineData("Engine.Tests/obj/Probe.csproj", true)]
    [InlineData("Engine.Tests/Http/bin/Probe.csproj", false)]
    [InlineData("shim/obj/Engine.Contracts.csproj", false)]
    [InlineData(".claude/worktrees/copy/Engine.Core/Engine.Core.csproj", true)]
    public void The_Gate_Leaves_Out_Only_The_Build_Output_At_The_Root_Of_A_Project(string relative, bool outOfScope)
    {
        string[] projects = ["Engine.Tests"];

        Assert.Equal(outOfScope, IsOutOfScope(relative, directory => projects.Contains(directory)));
    }

    // Finding T72 of the second codebase review of 2026-10-28 (TASK-0098): Engine.Core gives its
    // internals to the assembly Engine.Tests, and a client that took that assembly name read
    // them. Each assembly name is unique and is not the name of the file of another project,
    // and only a project file sets it, with no property. The four programs keep their names.
    // The item and the properties that name the file of the built assembly (T88).
    private static readonly HashSet<string> BuiltFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "IntermediateAssembly", "TargetName", "AssemblyName", "TargetPath", "TargetFileName",
    };

    internal static IEnumerable<string> AssemblyNameOffences(
        IEnumerable<(string Relative, XDocument Document)> projects, IEnumerable<(string Relative, XDocument Document)> imported)
    {
        var projectList = projects.ToList();
        var stems = projectList.Select(p => Path.GetFileNameWithoutExtension(p.Relative)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (relative, document) in imported)
        {
            if (document.Descendants().Any(e => Is(e, "AssemblyName")))
                yield return $"{relative} sets an AssemblyName; only a project file sets its own";
        }

        // Finding T80 of the codebase review of 2026-10-29 (TASK-0107): TargetName names the
        // built file, and the compiler takes the assembly name from that file. The desktop set
        // TargetName Engine.Tests and read the internals of the kernel. No file sets it.
        //
        // Finding T88 of the codebase review of 2026-10-30 (TASK-0112): a target that replaced the
        // item IntermediateAssembly gave the desktop the file Engine.Tests.dll and the internals of
        // the kernel. No file sets that item, and no task gives an output to it or to a property
        // that names the built file.
        foreach (var (relative, document) in projectList.Concat(imported))
        {
            if (document.Descendants().Any(e => Is(e, "TargetName")))
                yield return $"{relative} sets TargetName, which gives the assembly the name of its file; set AssemblyName in the project file";
            if (document.Descendants().Any(e => Is(e, "IntermediateAssembly")))
                yield return $"{relative} sets the item IntermediateAssembly, which names the file that the compiler builds";
            foreach (var output in document.Descendants().Where(e => Is(e, "Output")))
            {
                var target = Value(output, "ItemName") ?? Value(output, "PropertyName");
                if (target is not null && BuiltFileNames.Contains(target.Trim()))
                    yield return $"{relative} gives the output of a task to {target.Trim()}, which names the built assembly";
            }
        }

        foreach (var (relative, document) in projectList)
        {
            var stem = Path.GetFileNameWithoutExtension(relative);
            var declared = document.Descendants().Where(e => Is(e, "AssemblyName")).Select(e => e.Value.Trim()).ToList();
            if (declared.Count > 1)
                yield return $"{relative} sets AssemblyName {declared.Count} times";
            var name = declared.Count == 0 ? stem : declared[^1];
            if (name.IndexOfAny(['$', '@', '%']) >= 0)
                yield return $"{relative} has the AssemblyName '{name}', whose value MSBuild computes";
            else if (!string.Equals(name, stem, StringComparison.OrdinalIgnoreCase) && stems.Contains(name))
                yield return $"{relative} takes the assembly name {name}, which is the name of another project";
            if (names.TryGetValue(name, out var first))
                yield return $"{relative} and {first} have the same assembly name {name}";
            else
                names[name] = relative;
        }
    }

    [Fact]
    public void Each_Assembly_Name_Is_Unique_And_Names_No_Other_Project()
    {
        var projects = ProjectFiles().Select(f => (RepositoryFiles.Relative(f), XDocument.Load(f))).ToList();
        var imported = ProjectFiles()
            .SelectMany(f => FilesThatMsBuildReads(f).Where(file => !string.Equals(file, Path.GetFullPath(f), StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(f => (RepositoryFiles.Relative(f), XDocument.Load(f)));

        var offences = AssemblyNameOffences(projects, imported).ToArray();

        Assert.True(offences.Length == 0, "An assembly name that another project has reads the internals that the project gives to it.\n  " + string.Join("\n  ", offences));
    }

    [Theory]
    [InlineData("3DEngine/3DEngine.csproj", "<Project><PropertyGroup><AssemblyName>Engine.Tests</AssemblyName></PropertyGroup></Project>", "is the name of another project")]
    [InlineData("3DEngine/3DEngine.csproj", "<Project><PropertyGroup><assemblyname>engine.tests</assemblyname></PropertyGroup></Project>", "is the name of another project")]
    [InlineData("3DEngine/3DEngine.csproj", "<Project><PropertyGroup><AssemblyName>$(Friend)</AssemblyName></PropertyGroup></Project>", "whose value MSBuild computes")]
    [InlineData("3DEngine/3DEngine.csproj", "<Project><PropertyGroup><AssemblyName>engine</AssemblyName></PropertyGroup></Project>", "the same assembly name")]
    // Finding T80 (TASK-0107).
    [InlineData("3DEngine/3DEngine.csproj", "<Project><PropertyGroup><TargetName>Engine.Tests</TargetName></PropertyGroup></Project>", "sets TargetName")]
    [InlineData("3DEngine/3DEngine.csproj", "<Project><Target Name=\"T\"><PropertyGroup><targetname>Engine.Tests</targetname></PropertyGroup></Target></Project>", "sets TargetName")]
    // Finding T88 (TASK-0112): the form of the review, its case form, and the outputs of a task.
    [InlineData("3DEngine/3DEngine.csproj", "<Project><Target Name=\"ProbeRename\" BeforeTargets=\"CoreCompile\"><ItemGroup><IntermediateAssembly Remove=\"@(IntermediateAssembly)\" /><IntermediateAssembly Include=\"obj\\Debug\\net10.0\\Engine.Tests.dll\" /></ItemGroup></Target></Project>", "sets the item IntermediateAssembly")]
    [InlineData("3DEngine/3DEngine.csproj", "<Project><Target Name=\"T\"><ItemGroup><intermediateassembly Include=\"Engine.Tests.dll\" /></ItemGroup></Target></Project>", "sets the item IntermediateAssembly")]
    [InlineData("3DEngine/3DEngine.csproj", "<Project><Target Name=\"T\"><CreateItem Include=\"Engine.Tests.dll\"><Output TaskParameter=\"Include\" ItemName=\"IntermediateAssembly\" /></CreateItem></Target></Project>", "names the built assembly")]
    [InlineData("3DEngine/3DEngine.csproj", "<Project><Target Name=\"T\"><CreateProperty Value=\"Engine.Tests\"><Output TaskParameter=\"Value\" PropertyName=\" targetname \" /></CreateProperty></Target></Project>", "names the built assembly")]
    public void Each_Form_Of_Finding_T72_Is_An_Offence(string relative, string text, string expected)
    {
        (string, XDocument)[] projects =
        [
            ("Engine.Tests/Engine.Tests.csproj", XDocument.Parse("<Project />")),
            ("Engine.Cli/Engine.Cli.csproj", XDocument.Parse("<Project><PropertyGroup><AssemblyName>engine</AssemblyName></PropertyGroup></Project>")),
            (relative, XDocument.Parse(text)),
        ];

        Assert.Contains(AssemblyNameOffences(projects, []), offence => offence.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void An_Assembly_Name_In_An_Imported_File_Is_An_Offence()
    {
        (string, XDocument)[] imported = [("Directory.Build.props", XDocument.Parse("<Project><PropertyGroup><AssemblyName>Engine.Tests</AssemblyName></PropertyGroup></Project>"))];

        Assert.Single(AssemblyNameOffences([], imported));
        Assert.Single(AssemblyNameOffences([], [("3DEngine/3DEngine.csproj.user", XDocument.Parse("<Project><PropertyGroup><TargetName>Engine.Tests</TargetName></PropertyGroup></Project>"))]));
        Assert.Empty(AssemblyNameOffences([("Engine.Cli/Engine.Cli.csproj", XDocument.Parse("<Project><PropertyGroup><AssemblyName>engine</AssemblyName></PropertyGroup></Project>"))], []));
    }

    // The Unicode set Default_Ignorable_Code_Point (DerivedCoreProperties.txt, Unicode 16), with
    // U+034F, the combining grapheme joiner, which a text can use, and U+FEFF, which the rule of the
    // category Format holds with its exception for a byte order mark at the start.
    private static bool IsDefaultIgnorable(int value)
        => value is 0x00AD or 0x061C or 0x115F or 0x1160 or 0x17B4 or 0x17B5 or 0x3164 or 0xFFA0
            or (>= 0x180B and <= 0x180F) or (>= 0x200B and <= 0x200F) or (>= 0x202A and <= 0x202E)
            or (>= 0x2060 and <= 0x206F) or (>= 0xFE00 and <= 0xFE0F) or (>= 0xFFF0 and <= 0xFFF8)
            or (>= 0x1BCA0 and <= 0x1BCA3) or (>= 0x1D173 and <= 0x1D17A) or (>= 0xE0000 and <= 0xE0FFF);

    // Finding T77 of the second codebase review of 2026-10-28 (TASK-0098): a NUL byte in a
    // string literal made git show a test file as binary, so a person who reviewed a change
    // of it saw no text. A source file holds no control byte other than a tab and the line ends.
    //
    // Finding T86 of the codebase review of 2026-10-29 (TASK-0107): the gate read .cs files only,
    // and a NUL byte in a task file passed. A bidirectional control (U+202A to U+202E, U+2066
    // to U+2069) shows text in another order than the compiler reads it, and U+202E in a
    // comment passed. Each tracked text file holds neither.
    internal static IEnumerable<string> ControlByteOffences(string relative, byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            if ((bytes[i] < 0x20 && bytes[i] is not (0x09 or 0x0A or 0x0D)) || bytes[i] == 0x7F)
            {
                yield return $"{relative} holds the control byte 0x{bytes[i]:X2} at offset {i}; write an escape such as \\0";
                yield break;
            }

            // The UTF-8 bytes of U+202A to U+202E are E2 80 AA to E2 80 AE, and of U+2066 to
            // U+2069 are E2 81 A6 to E2 81 A9.
            if (bytes[i] == 0xE2 && i + 2 < bytes.Length
                && ((bytes[i + 1] == 0x80 && bytes[i + 2] is >= 0xAA and <= 0xAE) || (bytes[i + 1] == 0x81 && bytes[i + 2] is >= 0xA6 and <= 0xA9)))
            {
                yield return $"{relative} holds the bidirectional control U+{(bytes[i + 1] == 0x80 ? 0x2000 : 0x2040) + bytes[i + 2] - 0x80:X4} at offset {i}; write an escape such as \\u202E";
                yield break;
            }

            // Finding T92 of the codebase review of 2026-10-30 (TASK-0114): the line and paragraph
            // separators U+2028 and U+2029 (E2 80 A8 and A9) end a comment for the compiler and not
            // for a reader, and the marks of direction U+200E and U+200F (E2 80 8E and 8F), U+061C
            // (D8 9C) and U+0085 (C2 85) change the order or the lines that a reader sees.
            if ((bytes[i] == 0xE2 && i + 2 < bytes.Length && bytes[i + 1] == 0x80 && bytes[i + 2] is 0xA8 or 0xA9 or 0x8E or 0x8F)
                || (bytes[i] == 0xD8 && i + 1 < bytes.Length && bytes[i + 1] == 0x9C)
                || (bytes[i] == 0xC2 && i + 1 < bytes.Length && bytes[i + 1] == 0x85))
            {
                yield return $"{relative} holds a separator or a mark of direction at offset {i}; write an escape such as \\u2028";
                yield break;
            }

            // Finding T102 of the codebase review of 2026-10-30 (b) (TASK-0123): a CR with no LF after
            // it ends a line for the compiler, and a viewer can show the two lines as one; the C1
            // controls U+0080 to U+009F (C2 80 to C2 9F), the zero width space U+200B (E2 80 8B), the
            // word joiner U+2060 (E2 81 A0) and U+FEFF after the start (EF BB BF) do not show.
            if ((bytes[i] == 0x0D && (i + 1 >= bytes.Length || bytes[i + 1] != 0x0A))
                || (bytes[i] == 0xC2 && i + 1 < bytes.Length && bytes[i + 1] is >= 0x80 and <= 0x9F)
                || (bytes[i] == 0xE2 && i + 2 < bytes.Length && ((bytes[i + 1] == 0x80 && bytes[i + 2] == 0x8B) || (bytes[i + 1] == 0x81 && bytes[i + 2] == 0xA0)))
                || (i > 0 && bytes[i] == 0xEF && i + 2 < bytes.Length && bytes[i + 1] == 0xBB && bytes[i + 2] == 0xBF))
            {
                yield return $"{relative} holds a lone CR or a character that does not show at offset {i}; write an escape such as \\u200B";
                yield break;
            }
        }

        // Finding T108 of the codebase review of 2026-10-31 (TASK-0132): the bytes above are a list,
        // and other characters that do not show passed, as did bytes that are not UTF-8. A strict
        // decoder refuses an invalid or an overlong byte, and each character of the category Format
        // after the start, and each Hangul filler, is an offence.
        string decoded;
        try
        {
            decoded = new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (System.Text.DecoderFallbackException)
        {
            decoded = string.Empty;
        }
        if (decoded.Length == 0 && bytes.Length > 0)
        {
            yield return $"{relative} is not valid UTF-8; save it as UTF-8";
            yield break;
        }
        var offset = 0;
        foreach (var rune in decoded.EnumerateRunes())
        {
            var hidden = (System.Text.Rune.GetUnicodeCategory(rune) == System.Globalization.UnicodeCategory.Format && !(offset == 0 && rune.Value == 0xFEFF))
                || rune.Value is 0x115F or 0x1160 or 0x3164 or 0xFFA0 or (>= 0xFE00 and <= 0xFE0F)
                // Finding T113 of the codebase review of 2026-10-31 (b) (TASK-0143): the selectors of
                // variation of the supplement and of Mongolian do not show. U+034F and U+2800 stay text.
                || rune.Value is (>= 0xE0100 and <= 0xE01EF) or (>= 0x180B and <= 0x180F)
                // Finding T121 of the codebase review of 2026-10-31 (c) (TASK-0150): the rest of the
                // Unicode set Default_Ignorable_Code_Point, whose characters are not all of the
                // category Format; U+17B4 is valid inside a C# identifier.
                || IsDefaultIgnorable(rune.Value);
            if (hidden)
            {
                yield return $"{relative} holds the character U+{rune.Value:X4}, which does not show, at character {offset}; write an escape";
                yield break;
            }
            offset += rune.Utf16SequenceLength;
        }
    }

    // The tracked files that hold bytes and not text. A new binary type fails the gate until
    // it is in this list. ADR-0083 (TASK-0258): a binary STL file of the demonstration, testdata/stl, holds bytes.
    // ADR-0091 (TASK-0292): a font file is binary too.
    private static readonly string[] BinaryExtensions = [".spv", ".nupkg", ".png", ".ico", ".stl", ".ttf"];

    [Fact]
    public void No_Tracked_Text_File_Holds_A_Control_Byte()
    {
        var offences = TrackedFiles()
            .Where(relative => !BinaryExtensions.Contains(Path.GetExtension(relative), StringComparer.OrdinalIgnoreCase))
            .Where(relative => File.Exists(RepositoryFiles.Path(relative)))
            .SelectMany(relative => ControlByteOffences(relative, File.ReadAllBytes(RepositoryFiles.Path(relative))))
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    // Finding T177 of the codebase review of 2026-11-08 (b) (TASK-0302): a block of a conflict of git passed each gate, and in
    // that night a staged ADR held one. A line that is seven '<' or seven '>', alone or with a space and a name after them, or
    // seven '=' alone, is an offence in each tracked text file.
    [Fact]
    public void No_Tracked_Text_File_Holds_A_Conflict_Marker()
    {
        var offences = TrackedFiles()
            .Where(relative => !BinaryExtensions.Contains(Path.GetExtension(relative), StringComparer.OrdinalIgnoreCase))
            .Where(relative => File.Exists(RepositoryFiles.Path(relative)))
            .SelectMany(relative => ConflictMarkerOffences(relative, File.ReadAllText(RepositoryFiles.Path(relative))))
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    internal static IEnumerable<string> ConflictMarkerOffences(string relative, string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line is "<<<<<<<" or ">>>>>>>" or "======="
                || line.StartsWith("<<<<<<< ", StringComparison.Ordinal) || line.StartsWith(">>>>>>> ", StringComparison.Ordinal))
                yield return $"{relative}:{i + 1} holds a marker of a conflict of git";
        }
    }

    // T177: the forms of the markers that git writes, and lines that are not markers.
    [Theory]
    [InlineData("a\r\n<<<<<<< HEAD\r\nb\r\n", true)]
    [InlineData("a\n=======\nb\n", true)]
    [InlineData(">>>>>>> 5c3719d (K5d work in progress)\n", true)]
    [InlineData("<<<<<<< Updated upstream\n", true)]
    [InlineData("a <<<<<<< HEAD in a sentence\n", false)]
    [InlineData("========\n", false)]
    [InlineData("<<<<<<<<\n", false)]
    public void A_Conflict_Marker_Is_An_Offence(string text, bool offence)
        => Assert.Equal(offence, ConflictMarkerOffences("probe.md", text).Any());

    // The files that git tracks, from the git of PATH outside the checkout, as the judge of the
    // write set finds it (finding T29). No git, or a git that fails, fails the gate: an empty
    // list is not evidence.
    private static List<string> TrackedFiles()
    {
        var git = WriteSetCheck.Judge.FindGit(RepositoryFiles.Root, Environment.GetEnvironmentVariable("PATH"));
        Assert.True(git is not null, "The gate finds no git in a folder of PATH outside the checkout.");

        var start = new System.Diagnostics.ProcessStartInfo(git)
        {
            WorkingDirectory = Path.GetTempPath(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
        };
        foreach (var argument in new[] { "-C", RepositoryFiles.Root, "ls-files", "-z" })
            start.ArgumentList.Add(argument);

        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        var files = WriteSetCheck.Judge.ParseGitPaths(output.GetAwaiter().GetResult()).Order(StringComparer.Ordinal).ToList();

        Assert.True(process.ExitCode == 0 && files.Count > 0, $"git ls-files gave the exit code {process.ExitCode} and {files.Count} files.");
        return files;
    }

    [Theory]
    [InlineData(new byte[] { 0x61, 0x00, 0x62 }, true)]
    [InlineData(new byte[] { 0x61, 0x1B, 0x62 }, true)]
    [InlineData(new byte[] { 0x61, 0x7F }, true)]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x61, 0x09, 0x0D, 0x0A }, false)]
    // Finding T86: U+202E, U+202A, U+2066 and U+2069; U+2014 (an em dash, E2 80 94) and
    // U+2192 (an arrow, E2 86 92) are text.
    [InlineData(new byte[] { 0x2F, 0x2F, 0xE2, 0x80, 0xAE, 0x61 }, true)]
    [InlineData(new byte[] { 0xE2, 0x80, 0xAA }, true)]
    [InlineData(new byte[] { 0xE2, 0x81, 0xA6 }, true)]
    [InlineData(new byte[] { 0xE2, 0x81, 0xA9 }, true)]
    [InlineData(new byte[] { 0xE2, 0x80, 0x94, 0xE2, 0x86, 0x92 }, false)]
    // Finding T92 (TASK-0114): U+2028, U+2029, U+200E, U+200F, U+061C and U+0085; a dash U+2013
    // (E2 80 93) and an e with an acute accent (C3 A9) are text.
    [InlineData(new byte[] { 0x2F, 0x2F, 0x61, 0xE2, 0x80, 0xA8, 0x63 }, true)]
    [InlineData(new byte[] { 0xE2, 0x80, 0xA9 }, true)]
    [InlineData(new byte[] { 0xE2, 0x80, 0x8E }, true)]
    [InlineData(new byte[] { 0xE2, 0x80, 0x8F }, true)]
    [InlineData(new byte[] { 0xD8, 0x9C }, true)]
    [InlineData(new byte[] { 0xC2, 0x85 }, true)]
    [InlineData(new byte[] { 0xE2, 0x80, 0x93, 0xC3, 0xA9 }, false)]
    // Finding T102 (TASK-0123): a lone CR in the middle and at the end, U+009B, U+008D, U+200B,
    // U+2060 and U+FEFF after the start; a CR with its LF, a no-break space (C2 A0) and U+FEFF at
    // the start are text.
    [InlineData(new byte[] { 0x2F, 0x2F, 0x61, 0x0D, 0x23, 0x65 }, true)]
    [InlineData(new byte[] { 0x61, 0x0D }, true)]
    [InlineData(new byte[] { 0x61, 0xC2, 0x9B }, true)]
    [InlineData(new byte[] { 0xC2, 0x8D }, true)]
    [InlineData(new byte[] { 0xE2, 0x80, 0x8B }, true)]
    [InlineData(new byte[] { 0xE2, 0x81, 0xA0 }, true)]
    [InlineData(new byte[] { 0x61, 0xEF, 0xBB, 0xBF }, true)]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x61, 0x0D, 0x0A, 0xC2, 0xA0 }, false)]
    // Finding T108 (TASK-0132): U+00AD, U+200C, U+200D, U+2061, U+180E, U+FE0F, U+3164, U+E0041, a lone
    // byte 0x81 and the overlong form E0 82 85; an euro sign (E2 82 AC) is text.
    [InlineData(new byte[] { 0x61, 0xC2, 0xAD }, true)]
    [InlineData(new byte[] { 0xE2, 0x80, 0x8C }, true)]
    [InlineData(new byte[] { 0xE2, 0x80, 0x8D }, true)]
    [InlineData(new byte[] { 0xE2, 0x81, 0xA1 }, true)]
    [InlineData(new byte[] { 0xE1, 0xA0, 0x8E }, true)]
    [InlineData(new byte[] { 0xEF, 0xB8, 0x8F }, true)]
    [InlineData(new byte[] { 0xE3, 0x85, 0xA4 }, true)]
    [InlineData(new byte[] { 0xF3, 0xA0, 0x81, 0x81 }, true)]
    [InlineData(new byte[] { 0x61, 0x81 }, true)]
    [InlineData(new byte[] { 0xE0, 0x82, 0x85 }, true)]
    [InlineData(new byte[] { 0x61, 0xE2, 0x82, 0xAC }, false)]
    // Finding T113 (TASK-0143): U+E0100 and U+180B do not show; U+034F and U+2800 are text.
    [InlineData(new byte[] { 0x61, 0xF3, 0xA0, 0x84, 0x80 }, true)]
    [InlineData(new byte[] { 0x61, 0xE1, 0xA0, 0x8B }, true)]
    [InlineData(new byte[] { 0x61, 0xCD, 0x8F }, false)]
    [InlineData(new byte[] { 0x61, 0xE2, 0xA0, 0x80 }, false)]
    // Finding T121 of the codebase review of 2026-10-31 (c) (TASK-0150): U+17B4, which is valid inside
    // an identifier, U+2065 and U+FFF0 are default ignorable and do not show; U+17B6 is text.
    [InlineData(new byte[] { 0x61, 0xE1, 0x9E, 0xB4 }, true)]
    [InlineData(new byte[] { 0x61, 0xE2, 0x81, 0xA5 }, true)]
    [InlineData(new byte[] { 0x61, 0xEF, 0xBF, 0xB0 }, true)]
    [InlineData(new byte[] { 0x61, 0xE1, 0x9E, 0xB6 }, false)]
    public void A_Control_Byte_In_A_Source_File_Is_An_Offence(byte[] bytes, bool offence)
    {
        Assert.Equal(offence, ControlByteOffences("Probe.cs", bytes).Any());
    }

    // Each value of the attribute Include on each item with the given name, one
    // path at a time. An Include may hold several paths with a semicolon, and
    // an attribute may come in any order; XML reading handles both, and it
    // ignores a comment.
    private static IEnumerable<string> Includes(string file, string itemName, string attribute = "Include")
    {
        var document = XDocument.Load(file);

        foreach (var item in document.Descendants().Where(e => Is(e, itemName)))
        {
            var include = Value(item, attribute);
            if (include is null)
                continue;

            foreach (var part in include.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                yield return part;
        }
    }

    [Fact]
    public void Each_Project_Name_Appears_Exactly_Once()
    {
        // The graph is keyed by project name, therefore a duplicate name would
        // shadow a real project silently. This test makes that condition loud.
        var duplicates = ProjectFiles()
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} appears {g.Count()} times: "
                + string.Join(", ", g.Select(RepositoryFiles.Relative)))
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            "A project name appears more than once, therefore the dependency graph is not "
            + "trustworthy. Exclude the copy or give it its own name. Duplicates:\n  "
            + string.Join("\n  ", duplicates));
    }

    // MSBuild reads the name of an element and of an attribute with no regard to case,
    // so each gate that reads an MSBuild file does the same. Until TASK-0078 the gates
    // compared names with case, and a lower-case projectreference, compile item or
    // property that imports a file passed each gate while MSBuild used it (finding T55
    // of the codebase review of 2026-10-17).
    internal static bool Is(XElement element, string name)
        => string.Equals(element.Name.LocalName, name, StringComparison.OrdinalIgnoreCase);

    internal static string? Value(XElement element, string attribute)
        => element.Attributes().FirstOrDefault(a => string.Equals(a.Name.LocalName, attribute, StringComparison.OrdinalIgnoreCase))?.Value;

    // The files that MSBuild reads for one project: the project file, each file
    // that it imports, and each Directory.Build.props and Directory.Build.targets
    // in its directory and above it, up to the root of the repository. A
    // reference in any of them is a reference of the project. Until TASK-0047
    // the gate read the project file only, and a Directory.Build.props with a
    // reference to 3DEngine.Core passed (codebase review of 2026-10-04, finding
    // T9). MSBuild reads the first Directory.Build file above a project; the gate
    // reads each one, which is stricter. It also reads Directory.Packages.props
    // and the file <project>.user, which MSBuild imports for each project. An
    // import that the gate cannot resolve to one file is refused by
    // Each_Import_Names_One_File_That_The_Gate_Reads; until TASK-0066 the gate
    // skipped it, and a wildcard import and a .csproj.user file each carried a
    // reference that MSBuild saw and the gate did not (findings T16 and T46 of the
    // codebase reviews of 2026-10-04 and 2026-10-12).
    // The packages that a project references, in each file that MSBuild reads for it (T117). Finding T124 of the
    // codebase review of 2026-11-01 (TASK-0161): a GlobalPackageReference of central package management references
    // a package for each project, and the pin rules read PackageReference only, so it pinned the vision library into
    // Engine.Core with no failure.
    // Finding T144 of the codebase review of 2026-11-06 (TASK-0198): a PackageReference Update in a Directory.Build.targets
    // changed the version of the pin of ppy.SDL3-CS, and the pin rule passed, because it read Include only. An Update
    // names a package of the project too.
    private static IEnumerable<string> PackageReferences(string projectFile)
        => FilesThatMsBuildReads(projectFile).SelectMany(file => new[] { "Include", "Update" }
            .SelectMany(attribute => Includes(file, "PackageReference", attribute).Concat(Includes(file, "GlobalPackageReference", attribute))));

    internal static IEnumerable<string> FilesThatMsBuildReads(string projectFile)
    {
        var root = Path.GetFullPath(RepositoryFiles.Root).TrimEnd(Path.DirectorySeparatorChar);
        var queue = new Queue<string>();
        queue.Enqueue(Path.GetFullPath(projectFile));
        if (File.Exists(Path.GetFullPath(projectFile) + ".user"))
            queue.Enqueue(Path.GetFullPath(projectFile) + ".user");

        for (var directory = Path.GetDirectoryName(Path.GetFullPath(projectFile));
             directory is not null && directory.StartsWith(root, StringComparison.OrdinalIgnoreCase);
             directory = Path.GetDirectoryName(directory))
        {
            foreach (var name in new[] { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" })
            {
                var candidate = Path.Combine(directory, name);
                if (File.Exists(candidate))
                    queue.Enqueue(candidate);
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (queue.Count > 0)
        {
            var file = queue.Dequeue();
            if (!seen.Add(file))
                continue;

            yield return file;

            foreach (var (_, imported) in Imports(file))
            {
                if (imported is not null)
                    queue.Enqueue(imported);
            }
        }
    }

    // Each import of one MSBuild file, with the file that it names, or null when
    // the gate cannot resolve it to one file that exists: a property such as
    // $(MSBuildThisFileDirectory), a wildcard, or an absent file.
    private static IEnumerable<(string Path, string? File)> Imports(string file)
    {
        foreach (var import in XDocument.Load(file).Descendants().Where(e => Is(e, "Import")))
        {
            var path = Value(import, "Project") ?? string.Empty;
            if (path.Contains("$(", StringComparison.Ordinal) || path.IndexOfAny(['*', '?']) >= 0 || path.Trim().Length == 0)
            {
                yield return (path, null);
                continue;
            }

            var imported = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, path.Replace('\\', '/')));
            yield return (path, File.Exists(imported) ? imported : null);
        }
    }

    [Fact]
    public void Each_Import_Names_One_File_That_The_Gate_Reads()
    {
        var offences = ProjectFiles()
            .SelectMany(FilesThatMsBuildReads)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(file => Imports(file)
                .Where(import => import.File is null)
                .Select(import => $"{RepositoryFiles.Relative(file)} imports '{import.Path}', which the gate cannot resolve to one file"))
            .ToArray();

        Assert.True(
            offences.Length == 0,
            "An import that the gate cannot read can carry a reference that breaks the authority diagram. "
            + "Name one file by a path with no property and no wildcard.\n  " + string.Join("\n  ", offences));
    }

    // Finding T101 of the codebase review of 2026-10-30 (b) (TASK-0123): an item Compile compiles a
    // file of any extension, and the UI gate and the diagnostics scanner read the files *.cs only.
    // A file ProbeUi.txt with [DllImport("cimgui")] and <Compile Include="ProbeUi.txt" /> put
    // igNewFrame into engine.dll, and the UI gate passed. Each item Compile names .cs files only.
    // Finding T107 of the codebase review of 2026-10-31 (TASK-0132): an Output of a task with the item
    // name Compile added a file of another extension, and the check read the elements Compile only.
    internal static IEnumerable<string> CompileOffences(string file, XDocument document)
        => document.Descendants().Where(e => Is(e, "Compile"))
            .SelectMany(item => (Value(item, "Include") ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(part => !part.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(part => $"{file} compiles '{part}', which is not a .cs file")
            .Concat(document.Descendants().Where(e => Is(e, "Output") && Value(e, "ItemName") is { } name && IsCompileName(name))
                .Select(_ => $"{file} adds items Compile through the output of a task, which the gates cannot read"));

    // Finding T115 of the codebase review of 2026-10-31 (b) (TASK-0143): ItemName="$(I)" with
    // <I>Compile</I> passed, because the check compared the literal value. A name that MSBuild
    // computes is refused too, and the value is trimmed.
    // Finding T119 of the codebase review of 2026-10-31 (c) (TASK-0150): MSBuild reads the escape
    // Comp%69le as Compile, so each %XX is decoded before the check, as T71 does for a path.
    private static bool IsCompileName(string name) => IsItemName(name, "Compile");

    // True when MSBuild reads the name as one of the items, or computes the name.
    private static bool IsItemName(string name, params string[] items)
    {
        var unescaped = Uri.UnescapeDataString(name);
        return items.Any(item => string.Equals(unescaped.Trim(), item, StringComparison.OrdinalIgnoreCase))
            || unescaped.Contains("$(", StringComparison.Ordinal) || unescaped.Contains("@(", StringComparison.Ordinal);
    }

    // Finding T127 of the codebase review of 2026-11-03 (TASK-0170): a Target with CreateItem and an Output whose item
    // name is PackageReference pinned OpenCvSharp4 into Engine.Core, and the pin rules passed, because they read the
    // elements PackageReference and GlobalPackageReference only.
    // Finding T130 of the second codebase review of 2026-11-03 (TASK-0177): an Output whose item name is
    // ProjectReference made Engine.Core reference 3DEngine.Core, and an Output with Reference adds an assembly; the
    // graph reads the elements ProjectReference and Reference only. Each gate reads items as elements, and no project
    // of the repository adds an item through a task, so an Output of a task adds no item of any name. An Output with
    // PropertyName stays free.
    internal static IEnumerable<string> ItemOutputOffences(string file, XDocument document)
        => document.Descendants()
            .Where(e => Is(e, "Output") && Value(e, "ItemName") is not null)
            .Select(e => $"{file} adds items '{Value(e, "ItemName")}' through the output of a task, which the gates cannot read");

    [Fact]
    public void No_Output_Of_A_Task_Adds_An_Item()
    {
        var offences = ProjectFiles()
            .SelectMany(FilesThatMsBuildReads)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(file => ItemOutputOffences(RepositoryFiles.Relative(file), XDocument.Load(file)))
            .ToArray();

        Assert.True(offences.Length == 0, "The gates read the items of a project as elements only. Name each item in an item group.\n  " + string.Join("\n  ", offences));
    }

    // Finding T133 of the codebase review of 2026-11-05 (TASK-0182): an item ReferencePath in a target made Engine.Core
    // reference 3DEngine.Core, and each gate passed, because the gates read the items ProjectReference, Reference and the
    // package items only. Each item of each file that MSBuild reads is now one of the items that the repository uses, or
    // one that a gate reads; a new kind of item needs a change of this list and of the gate that reads it.
    internal static readonly string[] PermittedItems =
    [
        "Compile", "EmbeddedResource", "FrameworkReference", "GlobalPackageReference", "InternalsVisibleTo", "None",
        "PackageReference", "ProjectReference", "Reference", "Using",
        // Phase R14f2 (TASK-0229): Each_Project_That_The_Phone_Reaches_Supports_Android reads it.
        "SupportedPlatform",
    ];

    // Phase R14f2 (TASK-0229): the third run of the emulator ended on Console.ForegroundColor, which Android does not have,
    // because no project of the APK named Android as a platform, so the analyzer CA1416 saw nothing. Each project that the
    // program of the phone reaches names it, and the rule of zero warnings refuses such an API.
    [Fact]
    public void Each_Project_That_The_Phone_Reaches_Supports_Android()
    {
        var graph = Graph();
        var offences = new List<string>();
        foreach (var host in HostsWithNoSurface)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>(graph.GetValueOrDefault(host) ?? []);
            while (stack.Count > 0)
            {
                var next = stack.Pop();
                if (reached.Add(next))
                {
                    foreach (var further in graph.GetValueOrDefault(next) ?? [])
                        stack.Push(further);
                }
            }

            foreach (var name in reached.Order(StringComparer.Ordinal))
            {
                var file = ProjectFiles().Single(f => Path.GetFileNameWithoutExtension(f) == name);
                if (!FilesThatMsBuildReads(file).SelectMany(read => Includes(read, "SupportedPlatform")).Any(platform => string.Equals(platform, "android", StringComparison.OrdinalIgnoreCase)))
                    offences.Add($"{host} reaches {name}, whose project names no SupportedPlatform android");

                // Finding T156 of the codebase review of 2026-11-06 (d) (TASK-0242): an item SupportedPlatform Remove="android"
                // in a file of the project silenced CA1416, and the Include above still passed. A Remove or an Update of a
                // SupportedPlatform is an offence, because the gate reads the Include only.
                foreach (var read in FilesThatMsBuildReads(file))
                {
                    if (new[] { "Remove", "Update" }.Any(attribute => Includes(read, "SupportedPlatform", attribute).Any()))
                        offences.Add($"{host} reaches {name}, and {RepositoryFiles.Relative(read)} removes or updates a SupportedPlatform");

                    // Finding T162 of the codebase review of 2026-11-06 (e) (TASK-0251): a Condition on the item or on an
                    // element that holds it, or CA1416 in a NoWarn or a WarningsNotAsErrors, silenced the analyzer, and
                    // each gate passed. The step of eng/android/check-platforms.js reads the forms that MSBuild evaluates;
                    // this rule gives the same answer on the computer, before the pipeline.
                    var document = XDocument.Load(read);
                    if (document.Descendants().Where(e => Is(e, "SupportedPlatform")).Any(item => item.AncestorsAndSelf().Any(e => e.Attribute("Condition") is not null)))
                        offences.Add($"{host} reaches {name}, and {RepositoryFiles.Relative(read)} has a SupportedPlatform under a Condition");
                    foreach (var property in document.Descendants().Where(e => Is(e, "NoWarn") || Is(e, "WarningsNotAsErrors")))
                    {
                        if (property.Value.Contains("CA1416", StringComparison.OrdinalIgnoreCase))
                            offences.Add($"{host} reaches {name}, and {RepositoryFiles.Relative(read)} has CA1416 in {property.Name.LocalName}");
                    }
                }

                // Finding T166 of the codebase review of 2026-11-07 (TASK-0263): with no TreatWarningsAsErrors, CA1416 was a
                // warning only, and each step of the pipeline passed. Each project that the phone reaches sets it to true.
                if (!FilesThatMsBuildReads(file).Any(read => XDocument.Load(read).Descendants().Any(e => Is(e, "TreatWarningsAsErrors") && e.Attribute("Condition") is null
                        && e.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))))
                    offences.Add($"{host} reaches {name}, whose project does not set TreatWarningsAsErrors to true");

                // Finding T170 of the codebase review of 2026-11-07 (b) (TASK-0272): the rule above read one element, so a
                // Condition on its PropertyGroup, a later false, a property CodeAnalysisTreatWarningsAsErrors, or a mode or a
                // level of the analysis passed it, and only the step of the pipeline refused them. Each of those forms is an
                // offence in a file that MSBuild reads for a project that the phone reaches.
                foreach (var read in FilesThatMsBuildReads(file))
                {
                    foreach (var element in XDocument.Load(read).Descendants())
                    {
                        var local = element.Name.LocalName;
                        if (Is(element, "TreatWarningsAsErrors") && (element.AncestorsAndSelf().Any(e => e.Attribute("Condition") is not null)
                                || !element.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)))
                            offences.Add($"{host} reaches {name}, and {RepositoryFiles.Relative(read)} sets TreatWarningsAsErrors under a Condition or to a value other than true");
                        if (local.Equals("CodeAnalysisTreatWarningsAsErrors", StringComparison.OrdinalIgnoreCase)
                            || local.StartsWith("AnalysisMode", StringComparison.OrdinalIgnoreCase) || local.StartsWith("AnalysisLevel", StringComparison.OrdinalIgnoreCase))
                            offences.Add($"{host} reaches {name}, and {RepositoryFiles.Relative(read)} sets {local}, which can turn CA1416 into a warning or off");
                    }
                }

                // T162: the step of the pipeline asks MSBuild for each project that the phone reaches.
                if (!PlatformCheckProjects().Contains(name))
                    offences.Add($"{host} reaches {name}, which eng/android/check-platforms.js does not name");

                // T162: a pragma or an attribute in the code of the project silences the analyzer too.
                foreach (var source in Directory.EnumerateFiles(Path.GetDirectoryName(file)!, "*.cs", SearchOption.AllDirectories)
                             .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                 && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
                {
                    // Finding T169 of the codebase review of 2026-11-07 (b) (TASK-0272): a SuppressMessage over two lines, or under
                    // an alias, passed the rule of a pragma or an attribute on the line. No code of the phone names CA1416, so
                    // each line that names it is an offence.
                    if (File.ReadLines(source).Any(line => line.Contains("CA1416", StringComparison.OrdinalIgnoreCase)))
                        offences.Add($"{host} reaches {name}, and {RepositoryFiles.Relative(source)} names CA1416, which can silence it");

                    // Finding T165 of the codebase review of 2026-11-07 (TASK-0263): a pragma with no code silences each
                    // warning below it, CA1416 too, and the rule above read a line that names CA1416 only.
                    if (File.ReadLines(source).Any(line => BlanketPragma.IsMatch(line)))
                        offences.Add($"{host} reaches {name}, and {RepositoryFiles.Relative(source)} has a #pragma warning disable with no code");
                }
            }

            // Finding T174 of the codebase review of 2026-11-07 (c) (TASK-0281): the host itself. CA1416 in a NoWarn or a
            // WarningsNotAsErrors of the host, or a line of its code that names CA1416, silenced the analyzer in the host, and
            // each rule above read only the projects that the host reaches.
            var hostFile = ProjectFiles().Single(f => Path.GetFileNameWithoutExtension(f) == host);
            foreach (var read in FilesThatMsBuildReads(hostFile))
            {
                foreach (var property in XDocument.Load(read).Descendants().Where(e => Is(e, "NoWarn") || Is(e, "WarningsNotAsErrors")))
                {
                    if (property.Value.Contains("CA1416", StringComparison.OrdinalIgnoreCase))
                        offences.Add($"{host}: {RepositoryFiles.Relative(read)} has CA1416 in {property.Name.LocalName}");
                }

                // The review of Codex of v0.266 (TASK-0285): a switch of the analyzers with the value false in the host.
                foreach (var property in XDocument.Load(read).Descendants().Where(e => Is(e, "RunAnalyzers") || Is(e, "RunAnalyzersDuringBuild") || Is(e, "EnableNETAnalyzers")))
                {
                    if (property.Value.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
                        offences.Add($"{host}: {RepositoryFiles.Relative(read)} sets {property.Name.LocalName} to false");
                }

                // Finding T175 of the codebase review of 2026-11-08 (TASK-0296): a mode or a level of the analysis, or
                // CodeAnalysisTreatWarningsAsErrors, in the host, as rule T170 refuses them in the projects that it reaches.
                foreach (var element in XDocument.Load(read).Descendants())
                {
                    var local = element.Name.LocalName;
                    if (local.Equals("CodeAnalysisTreatWarningsAsErrors", StringComparison.OrdinalIgnoreCase)
                        || local.StartsWith("AnalysisMode", StringComparison.OrdinalIgnoreCase) || local.StartsWith("AnalysisLevel", StringComparison.OrdinalIgnoreCase))
                        offences.Add($"{host}: {RepositoryFiles.Relative(read)} sets {local}, which can turn CA1416 into a warning or off");
                }
            }

            // T175: the host makes CA1416 an error with a WarningsAsErrors under no Condition. Until TASK-0296 the removal of
            // that line passed each gate of the computer, and only the probe build of the pipeline (TASK-0285) refused it.
            // Finding T178 of the codebase review of 2026-11-08 (b) (TASK-0303): MSBuild keeps the last assignment, so a later
            // WarningsAsErrors with no CA1416 turned it back into a warning, and the rule above still passed. Each assignment in
            // a file that MSBuild reads for the host keeps $(WarningsAsErrors), and the last one of the host names CA1416.
            foreach (var read in FilesThatMsBuildReads(hostFile))
            {
                foreach (var reset in XDocument.Load(read).Descendants().Where(e => Is(e, "WarningsAsErrors")
                             && !e.Value.Contains("$(WarningsAsErrors)", StringComparison.OrdinalIgnoreCase)))
                    offences.Add($"{host}: {RepositoryFiles.Relative(read)} sets WarningsAsErrors to '{reset.Value.Trim()}', which drops the earlier codes");
            }

            if (XDocument.Load(hostFile).Descendants().LastOrDefault(e => Is(e, "WarningsAsErrors")) is not { } last
                || last.AncestorsAndSelf().Any(a => a.Attribute("Condition") is not null)
                || !last.Value.Contains("CA1416", StringComparison.OrdinalIgnoreCase))
                offences.Add($"{host}: the last WarningsAsErrors of the host does not name CA1416 under no Condition");

            foreach (var source in Directory.EnumerateFiles(Path.GetDirectoryName(hostFile)!, "*.cs", SearchOption.AllDirectories)
                         .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                             && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            {
                if (File.ReadLines(source).Any(line => line.Contains("CA1416", StringComparison.OrdinalIgnoreCase) || BlanketPragma.IsMatch(line)))
                    offences.Add($"{host}: {RepositoryFiles.Relative(source)} names CA1416 or has a #pragma warning disable with no code");
            }
        }

        // T162: an analyzer configuration file can set the severity of CA1416 for each project under its folder.
        foreach (var config in AnalyzerConfigFiles(RepositoryFiles.Root))
        {
            if (File.ReadAllText(config).Contains("CA1416", StringComparison.OrdinalIgnoreCase))
                offences.Add($"{RepositoryFiles.Relative(config)} names CA1416");

            // T165 (TASK-0263): the severity of a category, or of each rule, holds CA1416 with no name of it.
            if (File.ReadLines(config).Any(line => line.TrimStart().StartsWith("dotnet_analyzer_diagnostic.", StringComparison.OrdinalIgnoreCase)))
                offences.Add($"{RepositoryFiles.Relative(config)} sets the severity of a category or of each rule");
        }

        Assert.True(offences.Count == 0, string.Join("\n  ", offences));
    }

    // T165: "#pragma warning disable" with no code after it, or with a comment only.
    private static readonly Regex BlanketPragma = new(@"^\s*#\s*pragma\s+warning\s+disable\s*(//.*)?$", RegexOptions.Compiled);

    // T162: the names of the list "projects" of eng/android/check-platforms.js.
    private static HashSet<string> PlatformCheckProjects()
    {
        var text = RepositoryFiles.Read("eng", "android", "check-platforms.js");
        var start = text.IndexOf("const projects = [", StringComparison.Ordinal);
        var end = text.IndexOf("];", start, StringComparison.Ordinal);
        return Regex.Matches(text[start..end], "'([^']+)'").Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
    }

    // T162: each .editorconfig and each .globalconfig of the repository, outside the folders of Git and of a build.
    private static IEnumerable<string> AnalyzerConfigFiles(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var name = Path.GetFileName(file);
            if (name == ".editorconfig" || name.EndsWith(".globalconfig", StringComparison.OrdinalIgnoreCase))
                yield return file;
        }

        foreach (var folder in Directory.EnumerateDirectories(directory))
        {
            if (new[] { ".git", "bin", "obj", "node_modules" }.Contains(Path.GetFileName(folder), StringComparer.OrdinalIgnoreCase))
                continue;
            foreach (var file in AnalyzerConfigFiles(folder))
                yield return file;
        }
    }

    internal static IEnumerable<string> ItemOffences(string file, XDocument document)
        => document.Descendants().Where(e => Is(e, "ItemGroup"))
            .SelectMany(group => group.Elements())
            .Where(item => !PermittedItems.Any(name => Is(item, name)))
            .Select(item => $"{file} has the item '{item.Name.LocalName}', which no gate reads");

    [Fact]
    public void Each_Item_Of_A_Project_Is_One_That_A_Gate_Reads()
    {
        var offences = ProjectFiles()
            .SelectMany(FilesThatMsBuildReads)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(file => ItemOffences(RepositoryFiles.Relative(file), XDocument.Load(file)))
            .ToArray();

        Assert.True(offences.Length == 0, "A gate reads each kind of item that a project may hold. Use one of: "
            + string.Join(", ", PermittedItems) + ".\n  " + string.Join("\n  ", offences));
    }

    // The forms of finding T133, and the controls.
    [Theory]
    [InlineData("<Target Name=\"T\"><ItemGroup><ReferencePath Include=\"..\\3DEngine.Core\\bin\\3DEngine.Core.dll\" /></ItemGroup></Target>", true)]
    [InlineData("<Target Name=\"T\"><ItemGroup><ReferencePathWithRefAssemblies Include=\"Probe.dll\" /></ItemGroup></Target>", true)]
    [InlineData("<ItemGroup><Analyzer Include=\"Probe.dll\" /></ItemGroup>", true)]
    [InlineData("<ItemGroup><Content Include=\"Probe.txt\" /></ItemGroup>", true)]
    [InlineData("<ItemGroup><ProjectReference Include=\"..\\Engine.Contracts\\Engine.Contracts.csproj\" /><None Include=\"a.txt\"><Pack>true</Pack></None></ItemGroup>", false)]
    [InlineData("<ItemGroup><Using Include=\"Xunit\" /><Compile Remove=\"Probe.cs\" /></ItemGroup>", false)]
    public void An_Item_That_No_Gate_Reads_Is_An_Offence(string items, bool offence)
    {
        var document = XDocument.Parse($"<Project>{items}</Project>");

        Assert.Equal(offence, ItemOffences("Probe.csproj", document).Any());
    }

    // The forms of findings T127 and T130, their next forms, and the controls.
    [Theory]
    [InlineData("<CreateItem Include=\"OpenCvSharp4\"><Output TaskParameter=\"Include\" ItemName=\"PackageReference\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"OpenCvSharp4\"><Output TaskParameter=\"Include\" ItemName=\"globalpackagereference\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"OpenCvSharp4\"><Output TaskParameter=\"Include\" ItemName=\"$(I)\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"OpenCvSharp4\"><Output TaskParameter=\"Include\" ItemName=\"Package%52eference\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"..\\3DEngine.Core\\3DEngine.Core.csproj\"><Output TaskParameter=\"Include\" ItemName=\"ProjectReference\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"Probe.dll\"><Output TaskParameter=\"Include\" ItemName=\"Reference\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"Probe.dll\"><Output TaskParameter=\"Include\" ItemName=\"Analyzer\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"Probe.txt\"><Output TaskParameter=\"Include\" ItemName=\"Other\" /></CreateItem>", true)]
    [InlineData("<CreateProperty Value=\"1\"><Output TaskParameter=\"Value\" PropertyName=\"Probe\" /></CreateProperty>", false)]
    [InlineData("<PackageReference Include=\"OpenCvSharp4\" />", false)]
    [InlineData("<ProjectReference Include=\"..\\Engine.Contracts\\Engine.Contracts.csproj\" />", false)]
    public void An_Item_Through_The_Output_Of_A_Task_Is_An_Offence(string item, bool offence)
    {
        var document = XDocument.Parse($"<Project><Target Name=\"T\">{item}</Target></Project>");

        Assert.Equal(offence, ItemOutputOffences("Probe.csproj", document).Any());
    }

    [Fact]
    public void Each_Compile_Item_Names_CSharp_Files_Only()
    {
        var offences = ProjectFiles()
            .SelectMany(FilesThatMsBuildReads)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(file => CompileOffences(RepositoryFiles.Relative(file), XDocument.Load(file)))
            .ToArray();

        Assert.True(offences.Length == 0, "The gates read the .cs files only. Give each source the extension .cs.\n  " + string.Join("\n  ", offences));
    }

    // The form of finding T101 and two more, and the controls: a .cs file in either case.
    [Theory]
    [InlineData("<Compile Include=\"ProbeUi.txt\" />", true)]
    [InlineData("<Compile Include=\"Probe.cs;ProbeUi.txt\" />", true)]
    [InlineData("<Compile Include=\"$(Probe)\" />", true)]
    [InlineData("<Compile Include=\"Probe.cs\" />", false)]
    [InlineData("<Compile Include=\"Probe.CS\" Link=\"Probe.txt\" />", false)]
    // Finding T107 (TASK-0132): an Output of a task with the item name Compile, in either case.
    [InlineData("<CreateItem Include=\"ProbeUi2.txt\"><Output TaskParameter=\"Include\" ItemName=\"Compile\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"ProbeUi2.txt\"><Output TaskParameter=\"Include\" ItemName=\"compile\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"Probe.txt\"><Output TaskParameter=\"Include\" ItemName=\"Other\" /></CreateItem>", false)]
    // Finding T115 (TASK-0143): a name that a property or an item gives, and a name with spaces.
    [InlineData("<CreateItem Include=\"ProbeUi2.txt\"><Output TaskParameter=\"Include\" ItemName=\"$(I)\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"ProbeUi2.txt\"><Output TaskParameter=\"Include\" ItemName=\"@(I)\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"ProbeUi2.txt\"><Output TaskParameter=\"Include\" ItemName=\" Compile \" /></CreateItem>", true)]
    // Finding T119 of the codebase review of 2026-10-31 (c) (TASK-0150): an escape of MSBuild; MSBuild
    // reads Comp%69le as Compile.
    [InlineData("<CreateItem Include=\"ProbeUi2.txt\"><Output TaskParameter=\"Include\" ItemName=\"Comp%69le\" /></CreateItem>", true)]
    [InlineData("<CreateItem Include=\"ProbeUi2.txt\"><Output TaskParameter=\"Include\" ItemName=\"%43ompile\" /></CreateItem>", true)]
    public void A_Compile_Item_Of_Another_Extension_Is_An_Offence(string item, bool offence)
    {
        var document = XDocument.Parse($"<Project><ItemGroup>{item}</ItemGroup></Project>");

        Assert.Equal(offence, CompileOffences("Probe.csproj", document).Any());
    }

    // The properties that tell MSBuild to import a file that no Import element
    // names, or to read the Directory files or the extension files from another
    // place.
    private static readonly Regex ImportProperty = new(
        @"^(Custom(Before|After)\w*|DirectoryBuild\w*Path|DirectoryPackagesPropsPath|Import(DirectoryBuild|DirectoryPackages|ProjectExtension|ByWildcard|UserLocationsByWildcard)\w*|MSBuildProjectExtensionsPath|BaseIntermediateOutputPath|MSBuildUserExtensionsPath)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // The SDKs that a project of the repository may name. An SDK is a set of
    // files that MSBuild imports, and a package can be an SDK.
    private static readonly string[] PermittedSdks = ["Microsoft.NET.Sdk", "Microsoft.NET.Sdk.Web"];

    // The graph reads each Include of a ProjectReference as text. MSBuild
    // evaluates it. Until TASK-0073 a reference through a property, a reference
    // from a file that a property imports, and a reference from an extension file
    // in obj/ each passed the gate while MSBuild listed it (finding T50 of the
    // codebase review of 2026-10-13). Each form that the gate cannot read as text
    // is refused here.
    [Fact]
    public void Each_File_That_MsBuild_Reads_Names_Its_References_As_Text()
    {
        var offences = new List<string>();

        foreach (var file in ProjectFiles().SelectMany(FilesThatMsBuildReads).Distinct(StringComparer.OrdinalIgnoreCase))
            offences.AddRange(ReferenceOffences(RepositoryFiles.Relative(file), XDocument.Load(file)));

        foreach (var projectFile in ProjectFiles())
            offences.AddRange(ExtensionFileOffences(Path.Combine(Path.GetDirectoryName(projectFile)!, "obj")));

        Assert.True(
            offences.Count == 0,
            "A reference that the gate cannot read as text can break the authority diagram. Name each "
            + "project by a path with no property, and set no property that imports a file.\n  "
            + string.Join("\n  ", offences));
    }

    // The forms of finding T50, and the forms of the same kind that TASK-0073 adds.
    // Each one passed the gate of main.
    [Theory]
    [InlineData("<Project><PropertyGroup><RenderKernel>..\\3DEngine.Core\\3DEngine.Core.csproj</RenderKernel></PropertyGroup><ItemGroup><ProjectReference Include=\"$(RenderKernel)\" /></ItemGroup></Project>", "whose value MSBuild computes")]
    [InlineData("<Project><PropertyGroup><CustomAfterMicrosoftCommonTargets>..\\docs\\x.targets</CustomAfterMicrosoftCommonTargets></PropertyGroup></Project>", "sets the property CustomAfterMicrosoftCommonTargets")]
    [InlineData("<Project><PropertyGroup><DirectoryBuildPropsPath>..\\docs\\x.props</DirectoryBuildPropsPath></PropertyGroup></Project>", "sets the property DirectoryBuildPropsPath")]
    [InlineData("<Project><ItemGroup><Reference Include=\"3DEngine.Core\" /></ItemGroup></Project>", "references the assembly 3DEngine.Core")]
    [InlineData("<Project Sdk=\"Evil.Sdk/1.0\" />", "names the SDK Evil.Sdk/1.0")]
    // Finding T55 of the codebase review of 2026-10-17: the same forms in lower case,
    // which MSBuild reads (TASK-0078).
    [InlineData("<Project><itemgroup><projectreference include=\"$(RenderKernel)\" /></itemgroup></Project>", "whose value MSBuild computes")]
    [InlineData("<Project><propertygroup><customaftermicrosoftcommontargets>x.targets</customaftermicrosoftcommontargets></propertygroup></Project>", "sets the property customaftermicrosoftcommontargets")]
    [InlineData("<Project><itemgroup><reference include=\"3DEngine.Core\" /></itemgroup></Project>", "references the assembly 3DEngine.Core")]
    [InlineData("<Project sdk=\"Evil.Sdk\" />", "names the SDK Evil.Sdk")]
    // Finding T56: a wildcard in a project reference (TASK-0078).
    [InlineData("<Project><ItemGroup><ProjectReference Include=\"..\\3DEngine.Vulkan\\3DEngine.Vulka?.csproj\" /></ItemGroup></Project>", "with a wildcard")]
    [InlineData("<Project><Sdk Name=\"Other.Sdk\" /></Project>", "imports an SDK with the element Sdk")]
    [InlineData("<Project><Import Project=\"Sdk.targets\" Sdk=\"Third.Sdk\" /></Project>", "imports an SDK with the element Import")]
    // Finding T71 of the second codebase review of 2026-10-28 (TASK-0098).
    [InlineData("<Project><ItemGroup><PackageReference Include=\"ImGui%2ENET\" Version=\"1.91.6.1\" /></ItemGroup></Project>", "with an MSBuild escape")]
    [InlineData("<Project><ItemGroup><ProjectReference Include=\"..\\3DEngine.Core\\3DEngine%2ECore.csproj\" /></ItemGroup></Project>", "with an MSBuild escape")]
    public void Each_Form_Of_Finding_T50_Is_An_Offence(string project, string expected)
    {
        var offences = ReferenceOffences("Probe.csproj", XDocument.Parse(project)).ToList();

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
    }

    // Finding T124 of the codebase review of 2026-11-01 (TASK-0161): a GlobalPackageReference is a package of the
    // project for the pin rules, in each case that MSBuild reads.
    [Fact]
    public void A_Global_Package_Reference_Is_A_Package_Of_The_Project()
    {
        var file = Path.Combine(Directory.CreateTempSubdirectory("package-").FullName, "Probe.csproj");
        try
        {
            File.WriteAllText(file, "<Project><ItemGroup><GlobalPackageReference Include=\"OpenCvSharp4\" /><globalpackagereference include=\"Vortice.Vulkan\" /></ItemGroup></Project>");

            Assert.Equal(["OpenCvSharp4", "Vortice.Vulkan"], PackageReferences(file));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(file)!, recursive: true);
        }
    }

    // The third form of finding T50: an extension file in obj/ (TASK-0073).
    [Fact]
    public void A_Lower_Case_Project_Reference_Enters_The_Graph()
    {
        // Finding T55: the graph read only the element ProjectReference with this case,
        // and a reference from Engine.Cli to the render kernel in lower case passed each
        // rule (TASK-0078).
        var file = Path.Combine(Directory.CreateTempSubdirectory("reference-").FullName, "Probe.csproj");
        try
        {
            File.WriteAllText(file, "<Project><ItemGroup><projectreference include=\"..\\3DEngine.Core\\3DEngine.Core.csproj\" /></ItemGroup></Project>");

            Assert.Equal([@"..\3DEngine.Core\3DEngine.Core.csproj"], Includes(file, "ProjectReference"));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(file)!, recursive: true);
        }
    }

    [Fact]
    public void An_Extension_File_In_Obj_Is_An_Offence()
    {
        var obj = Directory.CreateTempSubdirectory("obj-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(obj, "Probe.csproj.nuget.g.props"), "<Project />");
            File.WriteAllText(Path.Combine(obj, "Probe.csproj.nuget.g.targets"), "<Project />");
            Assert.Empty(ExtensionFileOffences(obj));

            File.WriteAllText(Path.Combine(obj, "Probe.csproj.probe.props"), "<Project />");
            Assert.Single(ExtensionFileOffences(obj));
        }
        finally
        {
            Directory.Delete(obj, recursive: true);
        }
    }

    private static IEnumerable<string> ReferenceOffences(string relative, XDocument document)
    {
        if (document.Root is { } root && Value(root, "Sdk") is { } sdks)
        {
            foreach (var sdk in sdks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!PermittedSdks.Contains(sdk.Split('/')[0]))
                    yield return $"{relative} names the SDK {sdk}";
            }
        }

        foreach (var element in document.Descendants())
        {
            var name = element.Name.LocalName;
            var include = Value(element, "Include");

            if (include is not null && (include.Contains("$(", StringComparison.Ordinal) || include.Contains("@(", StringComparison.Ordinal) || include.Contains("%(", StringComparison.Ordinal)))
                yield return $"{relative} has the item {name} '{include}', whose value MSBuild computes";
            // Finding T71 of the second codebase review of 2026-10-28 (TASK-0098): MSBuild reads
            // ImGui%2ENET as ImGui.NET, and NuGet restored it.
            else if (include is not null && include.Contains('%', StringComparison.Ordinal))
                yield return $"{relative} has the item {name} '{include}' with an MSBuild escape, which the gate does not read as MSBuild does";
            else if (include is not null && Is(element, "ProjectReference") && include.IndexOfAny(['*', '?']) >= 0)
                yield return $"{relative} has the item {name} '{include}' with a wildcard, which the graph cannot read";
            else if (Is(element, "Reference") && include is not null)
                yield return $"{relative} references the assembly {include}, which the graph does not read";
            else if (ImportProperty.IsMatch(name))
                yield return $"{relative} sets the property {name}, which imports a file that the gate does not read";
            else if (Is(element, "Sdk") || (Is(element, "Import") && Value(element, "Sdk") is not null))
                yield return $"{relative} imports an SDK with the element {name}";
        }
    }

    // MSBuild imports each file obj/<project file>.*.props and .targets. NuGet
    // writes the two files .nuget.g.props and .nuget.g.targets at each restore;
    // each other such file is an offence.
    private static IEnumerable<string> ExtensionFileOffences(string obj)
    {
        if (!Directory.Exists(obj))
            yield break;

        foreach (var file in Directory.EnumerateFiles(obj))
        {
            var fileName = Path.GetFileName(file);
            if ((fileName.EndsWith(".props", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".targets", StringComparison.OrdinalIgnoreCase))
                && !fileName.Contains(".nuget.g.", StringComparison.OrdinalIgnoreCase))
                yield return $"{RepositoryFiles.Relative(file)} is a file that MSBuild imports from obj/";
        }
    }

    // Finding T63 of the codebase review of 2026-10-28 (TASK-0089): the graph keys a project
    // by its file name, and the gate reads no project under bin/ or obj/, so a reference to
    // shim/bin/Engine.Contracts.csproj entered the graph as Engine.Contracts while MSBuild built
    // the shim. Each reference must resolve, from the folder of its project as MSBuild does, to
    // a project file that the gate reads.
    [Fact]
    public void Each_Project_Reference_Resolves_To_A_Project_That_The_Gate_Reads()
    {
        var scanned = ProjectFiles().Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var offences = ProjectFiles()
            .SelectMany(project => ReferenceTargetOffences(
                project,
                FilesThatMsBuildReads(project).SelectMany(source => Includes(source, "ProjectReference")),
                scanned))
            .ToList();

        Assert.True(offences.Count == 0, "A reference must name a project that the gate reads (finding T63):\n  " + string.Join("\n  ", offences));
    }

    internal static IEnumerable<string> ReferenceTargetOffences(string projectFile, IEnumerable<string> includes, ISet<string> scanned)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(projectFile))!;
        foreach (var include in includes)
        {
            var target = Path.GetFullPath(Path.Combine(directory, include.Replace('\\', Path.DirectorySeparatorChar)));
            if (!scanned.Contains(target))
                yield return $"{RepositoryFiles.Relative(projectFile)} references {include}, which is not a project that the gate reads (under bin/ or obj/, outside the repository, or absent)";
        }
    }

    [Theory]
    [InlineData(@"..\shim\bin\Engine.Contracts.csproj", true)]
    [InlineData(@"..\Engine.Contracts\obj\Engine.Contracts.csproj", true)]
    [InlineData(@"..\..\outside\Engine.Contracts.csproj", true)]
    [InlineData(@"..\Engine.Contracts\Engine.Contracts.csproj", false)]
    public void A_Reference_Outside_The_Projects_Of_The_Gate_Is_An_Offence(string include, bool offence)
    {
        var project = RepositoryFiles.Path("Engine.Core", "Engine.Core.csproj");
        var scanned = ProjectFiles().Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(offence, ReferenceTargetOffences(project, [include], scanned).Any());
    }

    internal static Dictionary<string, List<string>> Graph()
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var file in ProjectFiles())
        {
            var project = Path.GetFileNameWithoutExtension(file);
            var targets = FilesThatMsBuildReads(file)
                .SelectMany(source => Includes(source, "ProjectReference"))
                .Select(path => Path.GetFileNameWithoutExtension(path.Replace('\\', '/')))
                .ToList();

            graph[project] = targets;
        }

        return graph;
    }

    [Fact]
    public void Engine_Contracts_Has_No_Project_Reference()
    {
        var graph = Graph();
        Assert.True(graph.ContainsKey("Engine.Contracts"), "Engine.Contracts.csproj was not found.");
        Assert.Empty(graph["Engine.Contracts"]);
    }

    // ADR-0057 item 1: the owned kernel references no project, so its identity cannot depend on a backend, the bus or a
    // client.
    [Fact]
    public void The_Owned_Kernel_References_No_Project()
    {
        var graph = Graph();
        Assert.True(graph.ContainsKey("Engine.Kernel"), "Engine.Kernel.csproj was not found.");
        Assert.Empty(graph["Engine.Kernel"]);
    }

    // Finding T139 of the codebase review of 2026-11-05 (b) (TASK-0192): ADR-0057 item 1 says that the owned kernel references
    // no project and no package, and the fact above read the projects only, so a package in Engine.Kernel passed each gate.
    // A package is code that the determinism gate does not read, so the kernel of identity, the engine and the contracts
    // reference no package, in each file that MSBuild reads for them.
    [Theory]
    [InlineData("Engine.Kernel")]
    [InlineData("Engine.Core")]
    [InlineData("Engine.Contracts")]
    public void A_Project_Of_The_Path_Of_The_Log_References_No_Package(string project)
    {
        var file = ProjectFiles().Single(path => string.Equals(Path.GetFileNameWithoutExtension(path), project, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(PackageReferences(file));
    }

    // ADR-0058 item 1 (phase K4b, TASK-0184): the engine reads the owned kernel for the faces of a body. No other project.
    [Fact]
    public void Engine_Core_References_Only_Engine_Contracts_And_The_Owned_Kernel()
    {
        var graph = Graph();
        Assert.Equal(["Engine.Contracts", "Engine.Kernel"], graph["Engine.Core"]);
    }

    [Fact]
    public void ThreeDEngine_Core_Has_No_Project_Reference()
    {
        // It is a peer kernel per ADR-0009.
        var graph = Graph();
        Assert.Empty(graph["3DEngine.Core"]);
    }

    [Fact]
    public void No_Engine_Project_References_The_Render_Kernel()
    {
        var offences = Graph()
            .Where(p => p.Key.StartsWith("Engine.", StringComparison.Ordinal))
            .Where(p => p.Value.Contains("3DEngine.Core"))
            .Select(p => $"{p.Key} references 3DEngine.Core")
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    [Fact]
    public void The_Render_Kernel_References_No_Engine_Project()
    {
        var offences = Graph()["3DEngine.Core"]
            .Where(t => t.StartsWith("Engine.", StringComparison.Ordinal))
            .Select(t => $"3DEngine.Core references {t}")
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    // Each project in the repository has one class. The design-truth kernel
    // and its backend, the render side, the clients, the verifier and the
    // packaging project. A project outside each class is a project that no
    // rule reads, which is how the vendored sample framework escaped the gate
    // before TASK-0027.
    // ADR-0057 item 1 (phase K4a, TASK-0183): the owned kernel of identity is in the class of the kernel.
    private static readonly string[] Kernel = ["Engine.Contracts", "Engine.Core", "Engine.Geometry.Manifold", "Engine.Kernel"];

    private static readonly string[] RenderSide = ["3DEngine.Core", "3DEngine.Vulkan"];

    // TASK-0038 split Engine.Api.Http into the surface library and the program
    // engine-api-http, Engine.Api.Http.Server.
    private static readonly string[] HeadlessClients = ["Engine.Cli", "Engine.Api.Http.Server"];

    // The HTTP and WebSocket surface as a library that a host mounts on its
    // session (ADR-0019 item 3, TASK-0038). It is not a client: a client
    // references it.
    private static readonly string[] Surfaces = ["Engine.Api.Http"];

    [Fact]
    public void The_Surface_Library_References_Only_Engine_Core_And_Engine_Contracts()
    {
        // ADR-0019 item 3. The library cannot name the native backend or the
        // render side; the host that mounts it chooses the backend.
        var graph = Graph();
        Assert.True(graph.ContainsKey("Engine.Api.Http"), "Engine.Api.Http.csproj was not found.");

        var offences = graph["Engine.Api.Http"]
            .Where(t => t is not ("Engine.Core" or "Engine.Contracts"))
            .Select(t => $"Engine.Api.Http references {t}; ADR-0019 item 3 permits Engine.Core and Engine.Contracts only")
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    // TASK-0027 added the desktop host. Before that task the gate did not read
    // it, and the host referenced the vendored sample framework with no report.
    // Phase R14e2 (ADR-0061 item 4, TASK-0226): the Android program is a host that draws with no surface.
    private static readonly string[] HostsThatDraw = ["3DEngine", "3DEngine.Android"];

    // The hosts that draw with no surface: Android has no runtime pack of ASP.NET Core (ADR-0072 item 1).
    private static readonly string[] HostsWithNoSurface = ["3DEngine.Android"];

    // ADR-0072 (phase R14e1, TASK-0222): the application of a host that draws, a library of the scene, the user interface
    // and the actions. A host that draws references it. It references the kernel of design truth and the render side
    // only, so that a host with no surface, the phone, can reference it (The_Application_References_No_Surface).
    private static readonly string[] Applications = ["3DEngine.App"];

    [Fact]
    public void A_Host_With_No_Surface_References_No_Surface()
    {
        // ADR-0061 item 4 and ADR-0072 item 1 (TASK-0226): the program of the phone references the kernel, the
        // native backend at its composition root (ADR-0014 section 4), the render side and the application, and no file
        // that MSBuild reads for it names a FrameworkReference.
        string[] permitted = ["Engine.Contracts", "Engine.Core", "Engine.Geometry.Manifold", .. RenderSide, .. Applications];
        var graph = Graph();
        var offences = new List<string>();
        foreach (var host in HostsWithNoSurface)
        {
            Assert.True(graph.ContainsKey(host), $"{host}.csproj was not found.");
            offences.AddRange(graph[host].Where(target => !permitted.Contains(target))
                .Select(target => $"{host} references {target}; ADR-0072 item 1 permits no surface on a host with no surface"));
            offences.AddRange(FrameworkOffences(host, graph));
        }

        Assert.True(offences.Count == 0, string.Join("\n  ", offences));
    }

    // Finding T145 of the codebase review of 2026-11-06 (b) (TASK-0224): the rules of ADR-0072 read the framework
    // references of the project only, and a framework flows through each project reference and comes with the web SDK.
    // So the web SDK on the application, or a FrameworkReference in Engine.Core, gave the application ASP.NET Core, and
    // each gate passed. The offences of the project and of each project that it reaches, in each file that MSBuild reads.
    private static IEnumerable<string> FrameworkOffences(string project, Dictionary<string, List<string>> graph)
    {
        var reached = new HashSet<string>(StringComparer.Ordinal) { project };
        var stack = new Stack<string>([project]);
        while (stack.Count > 0)
        {
            foreach (var next in graph.GetValueOrDefault(stack.Pop()) ?? [])
            {
                if (reached.Add(next))
                    stack.Push(next);
            }
        }

        foreach (var name in reached.Order(StringComparer.Ordinal))
        {
            var file = ProjectFiles().Single(f => Path.GetFileNameWithoutExtension(f) == name);
            foreach (var read in FilesThatMsBuildReads(file))
            {
                var relative = RepositoryFiles.Relative(read);
                foreach (var framework in Includes(read, "FrameworkReference"))
                    yield return $"{project} reaches the framework {framework} through {relative}; ADR-0072 item 1 permits none";
                if (XDocument.Load(read).Root is { } root && Value(root, "Sdk") is { } sdks
                    && sdks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Any(sdk => !string.Equals(sdk.Split('/')[0], "Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase)))
                    yield return $"{project} reaches the SDK {sdks} through {relative}; ADR-0072 item 1 permits Microsoft.NET.Sdk only";
            }
        }
    }

    [Fact]
    public void The_Application_References_No_Surface()
    {
        // ADR-0072 item 1: Android has no runtime pack of ASP.NET Core, so a program that references the surface
        // library, or the framework of ASP.NET Core, does not build for android-arm64 (NETSDK1082). The application
        // references Engine.Contracts, Engine.Core and the render side only, and no file that MSBuild reads for it names
        // a FrameworkReference.
        string[] permitted = ["Engine.Contracts", "Engine.Core", .. RenderSide];
        var graph = Graph();
        var offences = new List<string>();
        foreach (var application in Applications)
        {
            Assert.True(graph.ContainsKey(application), $"{application}.csproj was not found.");
            offences.AddRange(graph[application].Where(target => !permitted.Contains(target))
                .Select(target => $"{application} references {target}; ADR-0072 item 1 permits Engine.Contracts, Engine.Core and the render side only"));
            offences.AddRange(FrameworkOffences(application, graph));
        }

        Assert.True(offences.Count == 0, string.Join("\n  ", offences));
    }

    private static readonly string[] Verifiers = ["Engine.Tests"];

    // The tests of the render side, with no GPU (ADR-0022, TASK-0069). They may
    // reference the render side only; Engine.Tests references none of it.
    private static readonly string[] RenderVerifiers = ["3DEngine.Tests"];

    // The image test of the first demonstration, with a Vulkan device and no window
    // (ADR-0025 item 3, TASK-0076). It may reference the projects that a host that
    // draws references, and that host.
    private static readonly string[] ImageVerifiers = ["3DEngine.ImageTests"];

    // eng/manifold-native packs the native payload. It holds no code.
    // Phase R14d (ADR-0061 item 6, TASK-0196): the package of the native cimgui for Android is a packaging project too.
    private static readonly string[] Packaging = ["Engine.Geometry.Manifold.Native", "Engine.Native.DearImGui"];

    // eng/write-set-check judges the write set of each commit. The pipeline
    // builds it from main, alone (TASK-0053). eng/repo-gates runs the gates that
    // read only files of the repository, also from a build of main (TASK-0064).
    private static readonly string[] Judges = ["WriteSetCheck", "RepoGates"];

    // The verification spike of the vision library (phase V0, ADR-0042 item 4, TASK-0145).
    private static readonly string[] VisionVerifiers = ["3DEngine.VisionTests"];

    [Fact]
    public void The_Vision_Spike_References_No_Project()
    {
        // ADR-0042 items 3 and 5: the vision library stays out of the projects of the log, and the
        // spike depends on no project of the repository.
        var graph = Graph();
        Assert.True(graph.ContainsKey("3DEngine.VisionTests"), "3DEngine.VisionTests.csproj was not found.");
        Assert.Empty(graph["3DEngine.VisionTests"]);
    }

    [Fact]
    public void Only_The_Vision_Spike_Pins_The_Vision_Library()
    {
        // ADR-0042 item 5: each package of OpenCvSharp4 has its pin in the vision spike, so no
        // Engine.* project and no render-side project can reach the library.
        // Finding T117 of the codebase review of 2026-10-31 (c) (TASK-0150): the rule read the project
        // file only, and an imported .props file pinned the library into Engine.Core with no failure.
        var pins = ProjectFiles()
            .Where(f => PackageReferences(f).Any(name => name.StartsWith("OpenCvSharp", StringComparison.OrdinalIgnoreCase)))
            .Select(RepositoryFiles.Relative)
            .ToArray();

        Assert.True(
            pins.SequenceEqual(["3DEngine.VisionTests/3DEngine.VisionTests.csproj"]),
            $"OpenCvSharp4 must have its pins in 3DEngine.VisionTests/3DEngine.VisionTests.csproj only. "
            + $"Pins found: {(pins.Length == 0 ? "none" : string.Join(", ", pins))}");
    }

    [Fact]
    public void The_Judge_Of_The_Write_Set_Has_No_Project_Reference()
    {
        // The pipeline builds the judge from a checkout of main and nothing else.
        // A reference to a project would make the judge depend on code that the
        // judged commit can change, and would slow each run.
        var graph = Graph();
        Assert.True(graph.ContainsKey("WriteSetCheck"), "eng/write-set-check/WriteSetCheck.csproj was not found.");
        Assert.Empty(graph["WriteSetCheck"]);
    }

    [Fact]
    public void Each_Project_Is_In_One_Class()
    {
        // A new project must enter one of the lists above before the gate can
        // apply a rule to it. Until TASK-0039 an unlisted project passed each
        // test, because no test read it (rules review finding F22).
        string[] classified = [.. Kernel, .. RenderSide, .. HeadlessClients, .. Surfaces, .. HostsThatDraw, .. Applications, .. Verifiers, .. RenderVerifiers, .. ImageVerifiers, .. Packaging, .. Judges, .. VisionVerifiers];

        var unclassified = Graph().Keys
            .Where(p => !classified.Contains(p))
            .Select(p => $"{p} is in no class of DependencyDirectionGateTests")
            .ToArray();

        Assert.True(
            unclassified.Length == 0,
            "Each project must be a kernel, a render-side project, a headless client, a surface library, a host that "
            + "draws, an application, a verifier, a verifier of the render side, an image verifier, a packaging project, a judge or a vision verifier, so that the dependency rules apply to it. "
            + "Add it to one list.\n  " + string.Join("\n  ", unclassified));
    }

    [Fact]
    public void A_Client_References_Only_The_Permitted_Projects()
    {
        // A client may reference Engine.Core, Engine.Contracts, and
        // Engine.Geometry.Manifold at a composition root per ADR-0014 §4. A host
        // that draws may also reference the render side. ADR-0009 §2 forbids the
        // render kernel to a client that does not draw. Each client may also
        // reference the surface library and mount it on its session: the program
        // engine-api-http (ADR-0019 item 4, TASK-0038) and the desktop host
        // (ADR-0019 item 3, TASK-0054). Engine.Tests is a
        // verifier of authority and not a client.
        string[] permitted = ["Engine.Contracts", "Engine.Core", "Engine.Geometry.Manifold"];
        string[] clients = [.. HeadlessClients, .. HostsThatDraw];

        var graph = Graph();
        var offences = new List<string>();

        foreach (var client in clients)
        {
            Assert.True(graph.ContainsKey(client), $"{client}.csproj was not found.");
            var draws = HostsThatDraw.Contains(client);

            foreach (var target in graph[client])
            {
                // ADR-0072: a host that draws also references the application.
                if (!permitted.Contains(target) && !(draws && (RenderSide.Contains(target) || Applications.Contains(target))) && !Surfaces.Contains(target))
                    offences.Add($"{client} references {target}, which is not permitted");

                if (clients.Contains(target))
                    offences.Add($"{client} references the client {target}; clients do not reference each other");
            }
        }

        Assert.True(offences.Count == 0, string.Join("\n  ", offences));
    }

    [Fact]
    public void The_Vulkan_Layer_References_At_Most_The_Render_Kernel()
    {
        // ADR-0017 rule 2. The Vulkan layer draws render state and never sees
        // design truth. A host projects each event into render state (ADR-0009 §4).
        var graph = Graph();
        Assert.True(graph.ContainsKey("3DEngine.Vulkan"), "3DEngine.Vulkan.csproj was not found.");

        var offences = graph["3DEngine.Vulkan"]
            .Where(t => t != "3DEngine.Core")
            .Select(t => $"3DEngine.Vulkan references {t}; it may reference 3DEngine.Core only")
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    [Fact]
    public void A_Verifier_Of_The_Render_Side_References_Only_The_Render_Side()
    {
        // ADR-0022 item 3: the tests of the render side reference 3DEngine.Core
        // and 3DEngine.Vulkan, and nothing else.
        string[] permitted = ["3DEngine.Core", "3DEngine.Vulkan"];
        var graph = Graph();
        var offences = RenderVerifiers
            .Where(graph.ContainsKey)
            .SelectMany(verifier => graph[verifier]
                .Where(target => !permitted.Contains(target))
                .Select(target => $"{verifier} references {target}, which is not the render side"))
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    [Fact]
    public void An_Image_Verifier_References_Only_A_Host_That_Draws_And_The_Projects_Of_That_Host()
    {
        // ADR-0025 item 3: the projects that a client may reference, the render side,
        // the surface library, and the host that draws.
        string[] permitted = [.. Kernel, .. RenderSide, .. Surfaces, .. HostsThatDraw, .. Applications];
        var graph = Graph();
        var offences = ImageVerifiers
            .Select(verifier => graph.ContainsKey(verifier) ? null : $"{verifier}.csproj was not found")
            .OfType<string>()
            .Concat(ImageVerifiers
                .Where(graph.ContainsKey)
                .SelectMany(verifier => graph[verifier]
                    .Where(target => !permitted.Contains(target))
                    .Select(target => $"{verifier} references {target}, which is not a host that draws or a project of one")))
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    [Fact]
    public void Only_A_Host_That_Draws_References_The_Vulkan_Layer()
    {
        // ADR-0017 rule 3, ADR-0022 item 3 for the tests of the render side, and
        // ADR-0025 item 3 for the image test. This test also covers each Engine.*
        // project and the render kernel, because neither one is a host that draws.
        var offences = Graph()
            .Where(p => p.Value.Contains("3DEngine.Vulkan"))
            .Where(p => !HostsThatDraw.Contains(p.Key) && !Applications.Contains(p.Key) && !RenderVerifiers.Contains(p.Key) && !ImageVerifiers.Contains(p.Key))
            .Select(p => $"{p.Key} references 3DEngine.Vulkan, and it is not a host that draws")
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    // R14c (ADR-0061 item 5, TASK-0195): the old binding of SDL has no Android library, and two bindings of one native
    // library give two answers to "which SDL runs". No project references it, in each file that MSBuild reads.
    [Fact]
    public void No_Project_References_The_Old_Binding_Of_Sdl()
    {
        var offences = ProjectFiles()
            .Where(f => PackageReferences(f).Any(name => string.Equals(name, "Alimer.Bindings.SDL", StringComparison.OrdinalIgnoreCase)))
            .Select(RepositoryFiles.Relative)
            .ToArray();

        Assert.True(offences.Length == 0, "ADR-0061 item 5: ppy.SDL3-CS is the binding of SDL.\n  " + string.Join("\n  ", offences));
    }

    // Finding T146 of the codebase review of 2026-11-06 (b) (TASK-0224): the list below named its projects by hand, so a
    // new host that draws had no row, and a second binding of SDL in it passed, the form of T144 again. The rows name each
    // project of the render side, of the hosts that draw and of the applications, and no other one.
    [Fact]
    public void Each_Project_Of_The_Desktop_Has_A_Row_In_The_List_Of_The_Packages()
    {
        var method = typeof(DependencyDirectionGateTests).GetMethod(nameof(Each_Project_Of_The_Desktop_Names_Only_The_Packages_Of_Its_List))!;
        var rows = method.CustomAttributes
            .Where(attribute => attribute.AttributeType.Name == "InlineDataAttribute")
            .Select(attribute => (string)((IReadOnlyCollection<System.Reflection.CustomAttributeTypedArgument>)attribute.ConstructorArguments[0].Value!).First().Value!)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Order(StringComparer.Ordinal);

        Assert.Equal([.. RenderSide.Concat(HostsThatDraw).Concat(Applications).Order(StringComparer.Ordinal)], rows);
    }

    // Finding T144 (TASK-0198): a second binding of SDL with another name, SDL3-CS, in the host passed each gate. Each
    // project of the desktop names only the packages of its list, in each file that MSBuild reads for it, so a new package
    // there is a change of this list in review.
    [Theory]
    // ADR-0072: the pin of ImGui.NET moved from the desktop program to the application.
    [InlineData("3DEngine/3DEngine.csproj", new string[0])]
    [InlineData("3DEngine.App/3DEngine.App.csproj", new[] { "ImGui.NET" })]
    // Phase R14e2 (TASK-0226): the native cimgui of R14d for Android.
    [InlineData("3DEngine.Android/3DEngine.Android.csproj", new[] { "Engine.Native.DearImGui" })]
    [InlineData("3DEngine.Vulkan/3DEngine.Vulkan.csproj", new[] { "ppy.SDL3-CS", "Vortice.Vulkan" })]
    [InlineData("3DEngine.Core/3DEngine.Core.csproj", new string[0])]
    public void Each_Project_Of_The_Desktop_Names_Only_The_Packages_Of_Its_List(string project, string[] packages)
    {
        var others = PackageReferences(RepositoryFiles.Path(project))
            .Where(name => !packages.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(others.Length == 0, $"{project} names packages outside its list: {string.Join(", ", others)}.");
    }

    [Theory]
    [InlineData("Vortice.Vulkan")]
    // Since R14c (ADR-0061 item 5, TASK-0195) the binding of SDL is ppy.SDL3-CS on each platform.
    [InlineData("ppy.SDL3-CS")]
    public void Each_Binding_Has_One_Pin_In_The_Vulkan_Layer(string package)
    {
        // ADR-0017 rule 4. Before TASK-0027 three project files pinned the Vulkan
        // binding, therefore "which version does the repository use" had three
        // answers. A host receives each binding through its project reference.
        // Finding T117: each file that MSBuild reads for the project, as for the vision library.
        var pins = ProjectFiles()
            .Where(f => PackageReferences(f).Any(name => string.Equals(name, package, StringComparison.OrdinalIgnoreCase)))
            .Select(RepositoryFiles.Relative)
            .ToArray();

        Assert.True(
            pins.SequenceEqual(["3DEngine.Vulkan/3DEngine.Vulkan.csproj"]),
            $"{package} must have one pin, in 3DEngine.Vulkan/3DEngine.Vulkan.csproj. "
            + $"Pins found: {(pins.Length == 0 ? "none" : string.Join(", ", pins))}");
    }

    // Finding T148 of the codebase review of 2026-11-06 (c) (TASK-0231): the rule above counts projects, and a
    // Directory.Build.targets inside 3DEngine.Vulkan belongs to that project, so its PackageReference Update changed the
    // version of the pin and each gate passed. ADR-0017 rule 4: one file gives the version. Each file that MSBuild reads
    // for a project names a pinned package only when it is the file of the pin, by Include or by Update, in a
    // PackageReference, a GlobalPackageReference or a PackageVersion.
    [Theory]
    [InlineData("Vortice.Vulkan", "3DEngine.Vulkan/3DEngine.Vulkan.csproj")]
    [InlineData("ppy.SDL3-CS", "3DEngine.Vulkan/3DEngine.Vulkan.csproj")]
    [InlineData("ImGui.NET", "3DEngine.App/3DEngine.App.csproj")]
    [InlineData("Engine.Native.DearImGui", "3DEngine.Android/3DEngine.Android.csproj")]
    [InlineData("Engine.Geometry.Manifold.Native", "Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj")]
    [InlineData("OpenCvSharp4", "3DEngine.VisionTests/3DEngine.VisionTests.csproj")]
    public void Each_Pinned_Package_Is_Named_In_The_File_Of_Its_Pin_Only(string package, string pin)
    {
        var files = ProjectFiles()
            .SelectMany(FilesThatMsBuildReads)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(file => new[] { "PackageReference", "GlobalPackageReference", "PackageVersion" }
                .SelectMany(item => new[] { "Include", "Update" }.SelectMany(attribute => Includes(file, item, attribute)))
                .Any(name => string.Equals(Uri.UnescapeDataString(name), package, StringComparison.OrdinalIgnoreCase)))
            .Select(RepositoryFiles.Relative)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            files.SequenceEqual([pin]),
            $"{package} must be named in {pin} only, so that one file gives its version (ADR-0017 rule 4). "
            + $"Files that name it: {(files.Length == 0 ? "none" : string.Join(", ", files))}.");
    }

    // Finding T155 of the codebase review of 2026-11-06 (d) (TASK-0242): the rule above compares the text of a name, so a
    // property or an item list in the Update or the Remove of a package item named any package and passed. Such a name is
    // an offence in each file that MSBuild reads, as in an Include (ReferenceOffences).
    //
    // Finding T161 of the codebase review of 2026-11-06 (e) (TASK-0251): an Update with a wildcard, as
    // "Engine.Geometry.Manifold.Nativ?", changed the version of a pinned package, and each gate passed. A * or a ? in the
    // Update or the Remove of a package item is an offence too.
    [Fact]
    public void No_Package_Item_Updates_Or_Removes_By_A_Property_Or_A_Wildcard()
    {
        var offences = ProjectFiles()
            .SelectMany(FilesThatMsBuildReads)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(file => new[] { "PackageReference", "GlobalPackageReference", "PackageVersion" }
                .SelectMany(item => new[] { "Update", "Remove" }.SelectMany(attribute => Includes(file, item, attribute)
                    .Where(name => name.Contains("$(", StringComparison.Ordinal) || name.Contains("@(", StringComparison.Ordinal) || name.Contains("%(", StringComparison.Ordinal)
                        || name.Contains('*', StringComparison.Ordinal) || name.Contains('?', StringComparison.Ordinal))
                    .Select(name => $"{RepositoryFiles.Relative(file)}: {item} {attribute}=\"{name}\""))))
            .ToArray();

        Assert.True(offences.Length == 0, "A package item names its package by a property, an item list or a wildcard: " + string.Join("; ", offences));
    }

    [Fact]
    public void The_Native_Backend_References_Only_Engine_Contracts()
    {
        // ADR-0014 keeps the native dependency out of Engine.Core.
        var graph = Graph();
        Assert.Equal(["Engine.Contracts"], graph["Engine.Geometry.Manifold"]);
    }

    [Fact]
    public void The_Graph_Has_No_Cycle()
    {
        var graph = Graph();
        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        var cycles = new List<string>();

        foreach (var project in graph.Keys)
            Walk(project, []);

        void Walk(string project, List<string> path)
        {
            if (state.TryGetValue(project, out var seen) && seen == 2)
                return;

            if (seen == 1)
            {
                cycles.Add(string.Join(" -> ", path.Append(project)));
                return;
            }

            state[project] = 1;
            if (graph.TryGetValue(project, out var targets))
            {
                foreach (var target in targets)
                    Walk(target, [.. path, project]);
            }
            state[project] = 2;
        }

        Assert.True(cycles.Count == 0, "A reference cycle exists:\n  " + string.Join("\n  ", cycles));
    }
}
