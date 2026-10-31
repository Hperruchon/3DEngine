using Engine.Api.Http;
using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Core;

namespace Engine.Tests;

// Question Q4 of the codebase review of 2026-10-04 (finding E18, TASK-0140): a sink must not throw,
// by contract, and a test holds each sink to it. The bus appends the events of a commit after the
// Document changed, so a throw left a gap in the sequence, and nothing sent a reset.
public sealed class EventSinkContractTests
{
    // A new sink of the engine or of the surface must enter this list with a test below.
    private static readonly Type[] TestedSinks = [typeof(InMemoryEventSink), typeof(BroadcastingEventSink)];

    // Finding T112 of the codebase review of 2026-10-31 (b) (TASK-0143): the gate kept classes only and
    // read two assemblies, and DocumentSession takes a sink from each host. It keeps each type that is
    // not abstract and not an interface, in each assembly of a project of the repository that the test
    // project copies to its folder, except the test projects.
    [Fact]
    public void Each_Sink_Of_The_Engine_Has_A_Test_That_It_Does_Not_Throw()
    {
        var sinks = Sinks(ProjectAssemblies());

        Assert.Contains(typeof(EngineHost).Assembly, ProjectAssemblies());
        Assert.Contains(ProjectAssemblies(), assembly => assembly.GetName().Name == "engine");
        Assert.Equal(TestedSinks.OrderBy(type => type.FullName, StringComparer.Ordinal), sinks);
    }

    // Finding T118 of the codebase review of 2026-10-31 (c) (TASK-0150): the gate skipped a project whose
    // assembly is not in this folder with no message, and Engine.Tests does not reference the desktop.
    // Each project that can reach IEventSink is read here, or a test of another test project reads it.
    private static readonly Dictionary<string, string> ReadElsewhere = new(StringComparer.Ordinal)
    {
        ["3DEngine"] = "3DEngine.ImageTests/DesktopSinkContractTests.cs",
    };

    [Fact]
    public void Each_Project_That_Can_Hold_A_Sink_Is_Read_Here_Or_By_A_Named_Test()
    {
        var unread = ProjectsThatReachTheSink()
            .Where(name => !File.Exists(Path.Combine(AppContext.BaseDirectory, name + ".dll")))
            .ToArray();

        Assert.Equal(ReadElsewhere.Keys.Order(StringComparer.Ordinal), unread);
        Assert.All(ReadElsewhere.Values, test => Assert.True(File.Exists(Engine.Tests.Governance.RepositoryFiles.Path(test)), $"{test} does not exist."));
    }

    // The assembly of each project that is not a test and that is Engine.Core, the project of IEventSink,
    // or references it directly or through another project, in each file that MSBuild reads for it.
    private static string[] ProjectsThatReachTheSink()
    {
        var projects = Directory.EnumerateFiles(Engine.Tests.Governance.RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories)
            .GroupBy(file => Path.GetFileNameWithoutExtension(file), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var references = projects.ToDictionary(
            project => project.Key,
            project => Engine.Tests.Governance.DependencyDirectionGateTests.FilesThatMsBuildReads(project.Value)
                .SelectMany(file => System.Xml.Linq.XDocument.Load(file).Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
                .Select(element => Path.GetFileNameWithoutExtension(((string?)element.Attribute("Include") ?? string.Empty).Replace('\\', '/')))
                .ToArray(),
            StringComparer.Ordinal);

        bool Reaches(string project, HashSet<string> seen)
            => project == "Engine.Core" || (seen.Add(project) && references.GetValueOrDefault(project, []).Any(next => Reaches(next, seen)));

        return projects
            .Where(project => !project.Key.EndsWith("Tests", StringComparison.Ordinal) && Reaches(project.Key, []))
            .Select(project => System.Xml.Linq.XDocument.Load(project.Value).Descendants("AssemblyName").FirstOrDefault()?.Value ?? project.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    // The control of T112: a struct that implements IEventSink is a sink.
    [Fact]
    public void A_Struct_That_Implements_The_Sink_Is_A_Sink()
    {
        Assert.Contains(typeof(StructSink), Sinks([typeof(StructSink).Assembly]));
    }

    private static Type[] Sinks(IEnumerable<System.Reflection.Assembly> assemblies)
        => assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IEventSink).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

    // The assemblies of the projects of the repository that the build copies next to the tests. A
    // project can name its assembly: Engine.Cli builds engine.dll.
    private static System.Reflection.Assembly[] ProjectAssemblies()
        => Directory.EnumerateFiles(Engine.Tests.Governance.RepositoryFiles.Root, "*.csproj", SearchOption.AllDirectories)
            .Where(file => !Path.GetFileNameWithoutExtension(file).EndsWith("Tests", StringComparison.Ordinal))
            .Select(file => System.Xml.Linq.XDocument.Load(file).Descendants("AssemblyName").FirstOrDefault()?.Value ?? Path.GetFileNameWithoutExtension(file))
            .Where(name => File.Exists(Path.Combine(AppContext.BaseDirectory, name + ".dll")))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(name => System.Reflection.Assembly.Load(name))
            .ToArray();

    private readonly struct StructSink : IEventSink
    {
        public Task Append(EventRecord record, CancellationToken ct) => throw new InvalidOperationException("A struct sink throws.");

        public IReadOnlyList<EventRecord> Snapshot() => [];

        public int Count => 0;
    }

    // The ring evicts the oldest event when it is full, and the range that stays is contiguous.
    [Fact]
    public async Task The_Ring_Does_Not_Throw_When_It_Is_Full()
    {
        var sink = new InMemoryEventSink(capacity: 2);

        for (var seq = 1; seq <= 5; seq++)
            Assert.Null(await Record.ExceptionAsync(() => sink.Append(Event(seq), CancellationToken.None)));

        Assert.Equal([4L, 5L], sink.Snapshot().Select(record => record.Seq));
    }

    // An action of a subscription that throws is removed and reported (ADR-0024 item 1), and the append
    // continues into the ring.
    [Fact]
    public async Task The_Broadcasting_Sink_Does_Not_Throw_When_An_Action_Throws()
    {
        var broadcaster = new EventBroadcaster();
        var ring = new InMemoryEventSink();
        var sink = new BroadcastingEventSink(ring, broadcaster);
        using var failing = broadcaster.Listen(_ => throw new InvalidOperationException("a defect of the host"));

        Assert.Null(await Record.ExceptionAsync(() => sink.Append(Event(1), CancellationToken.None)));
        Assert.Null(await Record.ExceptionAsync(() => sink.Append(Event(2), CancellationToken.None)));

        Assert.Equal([1L, 2L], ring.Snapshot().Select(record => record.Seq));
    }

    private static EventRecord Event(long seq)
        => new(seq, DateTime.UnixEpoch, Guid.Empty, null, "command.applied", null);
}
