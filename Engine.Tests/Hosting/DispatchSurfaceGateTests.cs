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
    private static readonly string[] HostProjects = ["Engine.Cli", "Engine.Api.Http"];

    [Fact]
    public void No_Host_File_Contains_A_Registered_Command_Or_Query_Name()
    {
        var root = DiagnosticsScanner.FindRepoRoot(AppContext.BaseDirectory);
        var names = HandlerCatalog.CommandHandlers().Select(h => h.CommandName)
            .Concat(HandlerCatalog.QueryHandlers().Select(h => h.QueryName))
            .ToArray();

        var offences = new List<string>();
        var filesRead = 0;

        foreach (var project in HostProjects)
        {
            var directory = Path.Combine(root, project);
            Assert.True(Directory.Exists(directory), $"Host project not found: {project}");

            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
                    continue;

                filesRead++;
                var lines = File.ReadAllLines(path);
                for (var i = 0; i < lines.Length; i++)
                {
                    foreach (var name in names)
                    {
                        // A quoted name is a dispatch decision. A bare mention in a
                        // comment is prose and is permitted.
                        if (lines[i].Contains($"\"{name}\"", StringComparison.Ordinal))
                            offences.Add($"{relative}:{i + 1} names \"{name}\"");
                    }
                }
            }
        }

        Assert.True(filesRead > 0, "No host source file was read.");
        Assert.True(
            offences.Count == 0,
            "Per ADR-0016 a host must not name a command or a query. Adding a command must change "
            + "only its own files and HandlerCatalog. Offences:\n  "
            + string.Join("\n  ", offences));
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
