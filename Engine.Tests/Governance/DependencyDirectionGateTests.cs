using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// The gate that CLAUDE.md has listed for months with no check behind it.
// Register entry R-0004 recorded that gap. The working agreement of 2026-09-20
// called such a rule a wish rather than a rule.
//
// The gate reads each project file in the working tree. It needs no build and
// no reflection, therefore it also catches a reference that compiles.
public class DependencyDirectionGateTests
{
    private static readonly Regex ProjectReference = new(
        @"ProjectReference\s+Include\s*=\s*""([^""]+)""",
        RegexOptions.Compiled);

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

    private static Dictionary<string, List<string>> Graph()
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var file in ProjectFiles())
        {
            var project = Path.GetFileNameWithoutExtension(file);
            var targets = ProjectReference
                .Matches(File.ReadAllText(file))
                .Select(m => Path.GetFileNameWithoutExtension(
                    m.Groups[1].Value.Replace('\\', '/')))
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

    private static readonly string[] HeadlessClients = ["Engine.Cli", "Engine.Api.Http"];

    // TASK-0027 added the desktop host. Before that task the gate did not read
    // it, and the host referenced the vendored sample framework with no report.
    private static readonly string[] HostsThatDraw = ["3DEngine"];

    private static readonly string[] Verifiers = ["Engine.Tests"];

    // eng/manifold-native packs the native payload. It holds no code.
    private static readonly string[] Packaging = ["Engine.Geometry.Manifold.Native"];

    [Fact]
    public void Each_Project_Is_In_One_Class()
    {
        // A new project must enter one of the lists above before the gate can
        // apply a rule to it. Until TASK-0039 an unlisted project passed each
        // test, because no test read it (rules review finding F22).
        string[] classified = [.. Kernel, .. RenderSide, .. HeadlessClients, .. HostsThatDraw, .. Verifiers, .. Packaging];

        var unclassified = Graph().Keys
            .Where(p => !classified.Contains(p))
            .Select(p => $"{p} is in no class of DependencyDirectionGateTests")
            .ToArray();

        Assert.True(
            unclassified.Length == 0,
            "Each project must be a kernel, a render-side project, a headless client, a host that "
            + "draws, a verifier or a packaging project, so that the dependency rules apply to it. "
            + "Add it to one list.\n  " + string.Join("\n  ", unclassified));
    }

    [Fact]
    public void A_Client_References_Only_The_Permitted_Projects()
    {
        // A client may reference Engine.Core, Engine.Contracts, and
        // Engine.Geometry.Manifold at a composition root per ADR-0014 §4. A host
        // that draws may also reference the render side. ADR-0009 §2 forbids the
        // render kernel to a client that does not draw. Engine.Tests is a
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
                if (!permitted.Contains(target) && !(draws && RenderSide.Contains(target)))
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
    public void Only_A_Host_That_Draws_References_The_Vulkan_Layer()
    {
        // ADR-0017 rule 3. This test also covers each Engine.* project and the
        // render kernel, because neither one is a host that draws.
        var offences = Graph()
            .Where(p => p.Value.Contains("3DEngine.Vulkan"))
            .Where(p => !HostsThatDraw.Contains(p.Key))
            .Select(p => $"{p.Key} references 3DEngine.Vulkan, and it is not a host that draws")
            .ToArray();

        Assert.True(offences.Length == 0, string.Join("\n  ", offences));
    }

    private static readonly Regex PackageReference = new(
        @"PackageReference\s+Include\s*=\s*""([^""]+)""",
        RegexOptions.Compiled);

    [Theory]
    [InlineData("Vortice.Vulkan")]
    [InlineData("Alimer.Bindings.SDL")]
    public void Each_Binding_Has_One_Pin_In_The_Vulkan_Layer(string package)
    {
        // ADR-0017 rule 4. Before TASK-0027 three project files pinned the Vulkan
        // binding, therefore "which version does the repository use" had three
        // answers. A host receives each binding through its project reference.
        var pins = ProjectFiles()
            .Where(f => PackageReference.Matches(File.ReadAllText(f))
                .Any(m => string.Equals(m.Groups[1].Value, package, StringComparison.OrdinalIgnoreCase)))
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
