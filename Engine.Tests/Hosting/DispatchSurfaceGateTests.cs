using Engine.Core;
using Engine.Core.Hosting;
using Engine.Tests.Diagnostics;
using Xunit;

namespace Engine.Tests.Hosting;

// The gate for ADR-0016 and for the CLAUDE.md rule "a new command must not
// change a central file". That rule was inactive until this test existed,
// because two dispatch switch statements held each command name.
//
// The rule is mechanical: if a person adds a command name to a host, this
// test fails. Until TASK-0039 the gate read a fixed list of five files, and a
// new host file was not read (codebase review finding T2). It now reads each
// source file of each host project.
public class DispatchSurfaceGateTests
{
    // TASK-0038 moved the program engine-api-http into its own project; the
    // surface library and the program are each a host source.
    // Finding T4 of the codebase review of 2026-09-30 (R-0031, TASK-0451): the gate
    // read three projects by name, so the desktop host 3DEngine and the phone host
    // 3DEngine.Android were not read. The hosts now come from the classes of the
    // dependency gate, where each project must be.
    internal static IReadOnlyList<string> HostProjects => Governance.DependencyDirectionGateTests.DispatchHosts;

    [Fact]
    public void No_Host_File_Contains_A_Registered_Command_Or_Query_Name()
    {
        var root = DiagnosticsScanner.FindRepoRoot(AppContext.BaseDirectory);
        var names = HandlerCatalog.CommandHandlers().Select(h => h.CommandName)
            .Concat(HandlerCatalog.QueryHandlers().Select(h => h.QueryName))
            .ToArray();

        // The folder of a project is the folder of its tracked project file, which is not
        // always the name of the project: BookcaseCompare is in eng/bookcase-compare.
        var projectFiles = Governance.DependencyDirectionGateTests.TrackedFiles()
            .Where(relative => relative.EndsWith(".csproj", StringComparison.Ordinal))
            .ToList();

        var files = new List<(string Relative, string[] Lines)>();
        foreach (var project in HostProjects)
        {
            var found = projectFiles.Where(relative => Path.GetFileNameWithoutExtension(relative) == project).ToList();
            Assert.True(found.Count == 1, $"Host project {project}: {found.Count} project files, and the gate needs one.");
            var folder = Path.GetDirectoryName(found[0])!.Replace('\\', '/');

            foreach (var path in Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                if (!IsCompiled(folder, relative))
                    continue;

                files.Add((relative, File.ReadAllLines(path)));
            }
        }

        Assert.True(files.Count > 0, "No host source file was read.");
        var offences = Offences(files, names);
        Assert.True(
            offences.Count == 0,
            "Per ADR-0016 a host must not name a command or a query. Adding a command must change "
            + "only its own files and HandlerCatalog. Offences:\n  "
            + string.Join("\n  ", offences));
    }

    // MSBuild compiles each .cs file below the folder of a project except bin/ and obj/ at its
    // root (finding T68 of the second codebase review of 2026-10-28, TASK-0098). Until
    // TASK-0098 the gate skipped a folder bin or obj at each depth, so a host file in
    // Engine.Cli/Commands/bin/ named a command and passed.
    internal static bool IsCompiled(string folder, string relative)
        => !relative.StartsWith($"{folder}/obj/", StringComparison.Ordinal)
        && !relative.StartsWith($"{folder}/bin/", StringComparison.Ordinal);

    [Theory]
    [InlineData("Engine.Cli", "Engine.Cli/Commands/bin/Host.cs", true)]
    [InlineData("Engine.Cli", "Engine.Cli/Commands/obj/Host.cs", true)]
    [InlineData("Engine.Cli", "Engine.Cli/bin/Debug/Host.cs", false)]
    [InlineData("Engine.Api.Http", "Engine.Api.Http/obj/Debug/Engine.Api.Http.AssemblyInfo.cs", false)]
    [InlineData("eng/bookcase-compare", "eng/bookcase-compare/obj/Debug/BookcaseCompare.AssemblyInfo.cs", false)]
    public void The_Gate_Reads_Each_Host_Source_That_MSBuild_Compiles(string folder, string relative, bool compiled)
    {
        Assert.Equal(compiled, IsCompiled(folder, relative));
    }

    // Finding T4 of the codebase review of 2026-09-30 (R-0031, TASK-0451): the gate reads each
    // host of the classes of the dependency gate, the desktop host and the phone host too, and
    // not the application, which is the client of the person (DependencyDirectionGateTests.DispatchHosts).
    [Fact]
    public void The_Gate_Reads_Each_Host_Of_The_Dependency_Gate()
    {
        Assert.Equal(
            ["3DEngine", "3DEngine.Android", "BookcaseCompare", "Engine.Api.Http", "Engine.Api.Http.Server", "Engine.Cli"],
            HostProjects.Order(StringComparer.Ordinal));
    }

    // ADR-0028 item 9 (TASK-0085): the commands of R8a change central files of the engine one
    // time, and that exception does not reach the hosts. A host file that names RenameBody is an
    // offence, and the names of the gate come from the catalog, so they hold the new commands.
    [Fact]
    public void A_Host_File_That_Names_A_New_Command_Is_An_Offence()
    {
        var names = HandlerCatalog.CommandHandlers().Select(h => h.CommandName)
            .Concat(HandlerCatalog.QueryHandlers().Select(h => h.QueryName))
            .ToArray();
        Assert.Contains("RenameBody", names);
        Assert.Contains("SetBodyVisibility", names);
        Assert.Contains("GetBodyInfo", names);

        string[] injected = ["// RenameBody in a comment is prose.", "if (request.Name == \"RenameBody\") Rename(request);"];
        var offence = Assert.Single(Offences([("Engine.Api.Http/Endpoints/Injected.cs", injected)], names));

        Assert.Equal("Engine.Api.Http/Endpoints/Injected.cs:2 names \"RenameBody\"", offence);
    }

    // ADR-0033 item 12 (TASK-0100): the sketch commands change central files of the engine one
    // time, as the commands of R8a did, and that exception does not reach the hosts either.
    [Fact]
    public void A_Host_File_That_Names_A_Sketch_Command_Is_An_Offence()
    {
        var names = HandlerCatalog.CommandHandlers().Select(h => h.CommandName)
            .Concat(HandlerCatalog.QueryHandlers().Select(h => h.QueryName))
            .ToArray();
        Assert.Contains("CreateSketch", names);
        Assert.Contains("GetSketch", names);

        string[] injected = ["var command = new { name = \"CreateSketch\" };", "// GetSketch in a comment is prose."];
        var offence = Assert.Single(Offences([("Engine.Api.Http.Server/Injected.cs", injected)], names));

        Assert.Equal("Engine.Api.Http.Server/Injected.cs:1 names \"CreateSketch\"", offence);
    }

    // A quoted name is a dispatch decision. A bare mention in a comment is prose and is permitted.
    private static List<string> Offences(IEnumerable<(string Relative, string[] Lines)> files, IReadOnlyList<string> names)
    {
        var offences = new List<string>();
        foreach (var (relative, lines) in files)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var name in names)
                {
                    if (lines[i].Contains($"\"{name}\"", StringComparison.Ordinal))
                        offences.Add($"{relative}:{i + 1} names \"{name}\"");
                }
            }
        }

        return offences;
    }

    [Fact]
    public void Catalog_Registers_Without_A_Duplicate()
    {
        var commands = new CommandRegistry();
        var queries = new QueryRegistry();

        HandlerCatalog.RegisterAll(commands, queries);

        Assert.Equal(HandlerCatalog.CommandHandlers().Count, commands.Count);
        Assert.Equal(HandlerCatalog.QueryHandlers().Count, queries.Count);
    }

    [Fact]
    public void Catalog_Order_Is_Stable_Across_Calls()
    {
        // Replay determinism needs a fixed set and a fixed order. An assembly
        // scan would not give either one.
        var first = HandlerCatalog.CommandHandlers().Select(h => h.CommandName).ToArray();
        var second = HandlerCatalog.CommandHandlers().Select(h => h.CommandName).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Every_Catalog_Command_Declares_A_Create_That_Round_Trips_Its_Schema()
    {
        // Each handler must build its own command from the fields that it
        // declares. A field that Create ignores is a declaration that lies.
        foreach (var handler in HandlerCatalog.CommandHandlers())
        {
            var raw = handler.Parameters.ToDictionary(
                p => p.Key,
                p => SampleFor(p.Value.Type),
                StringComparer.Ordinal);

            var bound = ParameterBinder.Bind(handler.Parameters, raw);
            Assert.True(bound.IsSuccess, $"{handler.CommandName}: {bound.FirstMessage}");

            var id = Guid.NewGuid();
            var command = handler.Create(
                new Contracts.Handlers.CommandInput(bound.Values!, id, null));

            Assert.Equal(handler.CommandName, command.Name);
            Assert.Equal(handler.SchemaVersion, command.SchemaVersion);
            Assert.Equal(id, command.CommandId);
        }
    }

    private static object? SampleFor(string type) => type switch
    {
        "number" => "1.5",
        "integer" => "2",
        "boolean" => "true",
        "guid" => Guid.NewGuid().ToString(),
        "datetime" => "2026-09-20T00:00:00Z",
        _ => "sample",
    };
}
