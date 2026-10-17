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
    private static bool IsOutOfScope(string relative)
        => relative.Contains("/obj/", StringComparison.Ordinal)
        || relative.Contains("/bin/", StringComparison.Ordinal)
        || relative.StartsWith(".claude/", StringComparison.Ordinal);

    private static List<string> ProjectFiles()
        => Directory
            .EnumerateFiles(RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !IsOutOfScope(RepositoryFiles.Relative(f)))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

    // Each value of the attribute Include on each item with the given name, one
    // path at a time. An Include may hold several paths with a semicolon, and
    // an attribute may come in any order; XML reading handles both, and it
    // ignores a comment.
    private static IEnumerable<string> Includes(string file, string itemName)
    {
        var document = XDocument.Load(file);

        foreach (var item in document.Descendants().Where(e => Is(e, itemName)))
        {
            var include = Value(item, "Include");
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
    public void Each_Form_Of_Finding_T50_Is_An_Offence(string project, string expected)
    {
        var offences = ReferenceOffences("Probe.csproj", XDocument.Parse(project)).ToList();

        Assert.True(
            offences.Any(o => o.Contains(expected, StringComparison.Ordinal)),
            $"Expected an offence with '{expected}'. Offences:\n  " + string.Join("\n  ", offences));
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

    private static Dictionary<string, List<string>> Graph()
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

    [Fact]
    public void Engine_Core_References_Only_Engine_Contracts()
    {
        var graph = Graph();
        Assert.Equal(["Engine.Contracts"], graph["Engine.Core"]);
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
    private static readonly string[] Kernel = ["Engine.Contracts", "Engine.Core", "Engine.Geometry.Manifold"];

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
    private static readonly string[] HostsThatDraw = ["3DEngine"];

    private static readonly string[] Verifiers = ["Engine.Tests"];

    // The tests of the render side, with no GPU (ADR-0022, TASK-0069). They may
    // reference the render side only; Engine.Tests references none of it.
    private static readonly string[] RenderVerifiers = ["3DEngine.Tests"];

    // The image test of the first demonstration, with a Vulkan device and no window
    // (ADR-0025 item 3, TASK-0076). It may reference the projects that a host that
    // draws references, and that host.
    private static readonly string[] ImageVerifiers = ["3DEngine.ImageTests"];

    // eng/manifold-native packs the native payload. It holds no code.
    private static readonly string[] Packaging = ["Engine.Geometry.Manifold.Native"];

    // eng/write-set-check judges the write set of each commit. The pipeline
    // builds it from main, alone (TASK-0053). eng/repo-gates runs the gates that
    // read only files of the repository, also from a build of main (TASK-0064).
    private static readonly string[] Judges = ["WriteSetCheck", "RepoGates"];

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
        string[] classified = [.. Kernel, .. RenderSide, .. HeadlessClients, .. Surfaces, .. HostsThatDraw, .. Verifiers, .. RenderVerifiers, .. ImageVerifiers, .. Packaging, .. Judges];

        var unclassified = Graph().Keys
            .Where(p => !classified.Contains(p))
            .Select(p => $"{p} is in no class of DependencyDirectionGateTests")
            .ToArray();

        Assert.True(
            unclassified.Length == 0,
            "Each project must be a kernel, a render-side project, a headless client, a surface library, a host that "
            + "draws, a verifier, a verifier of the render side, an image verifier or a packaging project, so that the dependency rules apply to it. "
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
                if (!permitted.Contains(target) && !(draws && RenderSide.Contains(target)) && !Surfaces.Contains(target))
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
        string[] permitted = [.. Kernel, .. RenderSide, .. Surfaces, .. HostsThatDraw];
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
            .Where(p => !HostsThatDraw.Contains(p.Key) && !RenderVerifiers.Contains(p.Key) && !ImageVerifiers.Contains(p.Key))
            .Select(p => $"{p.Key} references 3DEngine.Vulkan, and it is not a host that draws")
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    [Theory]
    [InlineData("Vortice.Vulkan")]
    [InlineData("Alimer.Bindings.SDL")]
    public void Each_Binding_Has_One_Pin_In_The_Vulkan_Layer(string package)
    {
        // ADR-0017 rule 4. Before TASK-0027 three project files pinned the Vulkan
        // binding, therefore "which version does the repository use" had three
        // answers. A host receives each binding through its project reference.
        var pins = ProjectFiles()
            .Where(f => Includes(f, "PackageReference")
                .Any(name => string.Equals(name, package, StringComparison.OrdinalIgnoreCase)))
            .Select(RepositoryFiles.Relative)
            .ToArray();

        Assert.True(
            pins.SequenceEqual(["3DEngine.Vulkan/3DEngine.Vulkan.csproj"]),
            $"{package} must have one pin, in 3DEngine.Vulkan/3DEngine.Vulkan.csproj. "
            + $"Pins found: {(pins.Length == 0 ? "none" : string.Join(", ", pins))}");
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
