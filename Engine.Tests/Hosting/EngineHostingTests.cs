using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Xunit;

namespace Engine.Tests.Hosting;

// Salvaged from branch claude/happy-booth-1cef3f, which held a factory that
// nobody merged. Register entry R-0012 recorded that branch.
//
// Each test that named a command by hand now compares the kit against
// HandlerCatalog instead. The reason: a name in two positions drifts, and
// ADR-0016 makes the catalog the one list.
//
// The four tests for RegisterDefaultCommands and RegisterDefaultQueries are
// deliberately not salvaged. Those methods were a second registration surface,
// which is the defect that register entry R-0003 recorded.
public class EngineHostingTests
{
    private static EngineKit Kit() => EngineHosting.CreateDefault(new InProcessMeshBackend());

    [Fact]
    public void CreateDefault_Returns_A_Fresh_Document_Each_Call()
    {
        Assert.NotEqual(Kit().Document.DocumentId, Kit().Document.DocumentId);
    }

    [Fact]
    public void CreateDefault_Returns_Independent_Registries_Each_Call()
    {
        var a = Kit();
        var b = Kit();
        Assert.NotSame(a.CommandRegistry, b.CommandRegistry);
        Assert.NotSame(a.QueryRegistry, b.QueryRegistry);
        Assert.NotSame(a.Events, b.Events);
    }

    [Fact]
    public void CreateDefault_Registers_Exactly_The_Catalog()
    {
        var kit = Kit();

        Assert.Equal(
            HandlerCatalog.CommandHandlers().Select(h => (h.CommandName, h.SchemaVersion)).ToArray(),
            kit.CommandRegistry.Registered.ToArray());

        Assert.Equal(
            HandlerCatalog.QueryHandlers().Select(h => (h.QueryName, h.SchemaVersion)).ToArray(),
            kit.QueryRegistry.Registered.ToArray());
    }

    [Fact]
    public void CreateDefault_Keeps_The_Backend_That_The_Caller_Gave()
    {
        // The factory must not choose a backend. ADR-0014 section 4 puts that
        // choice at the composition root of a host.
        var backend = new InProcessMeshBackend();
        Assert.Same(backend, EngineHosting.CreateDefault(backend).Backend);
    }

    [Fact]
    public void CreateDefault_Refuses_A_Null_Backend()
    {
        Assert.Throws<ArgumentNullException>(() => EngineHosting.CreateDefault(null!));
    }

    [Fact]
    public void CreateCommandBus_Uses_The_Document_And_The_Backend_Of_The_Kit()
    {
        var kit = Kit();
        Assert.Same(kit.Document, kit.CreateCommandBus().Document);
    }

    [Fact]
    public void CreateCommandBus_Accepts_A_Decorated_Sink()
    {
        // The HTTP host needs this overload, because its bus must see the
        // broadcasting sink and not the plain one.
        var kit = Kit();
        var counting = new CountingEventSink();
        Assert.NotNull(kit.CreateCommandBus(counting));
        Assert.Throws<ArgumentNullException>(() => kit.CreateCommandBus(null!));
    }

    [Fact]
    public async Task A_Bus_From_The_Kit_Applies_A_Command_And_Announces_It()
    {
        // The one test that proves the parts compose, and not only that they exist.
        var kit = Kit();
        var bus = kit.CreateCommandBus();
        var handler = HandlerCatalog.CommandHandlers()[0];

        var command = handler.Create(new Contracts.Handlers.CommandInput(
            handler.Parameters.ToDictionary(p => p.Key, _ => (object?)"salvage"),
            Guid.NewGuid(),
            null));

        var result = await bus.Apply(command);

        Assert.Equal(Contracts.CommandStatus.Applied, result.Status);
        Assert.Equal(1, kit.Document.Version);
        Assert.NotEmpty(kit.Events.Snapshot());
    }

    private sealed class CountingEventSink : IEventSink
    {
        private readonly List<Contracts.EventRecord> _records = [];

        public Task Append(Contracts.EventRecord record, CancellationToken ct = default)
        {
            _records.Add(record);
            return Task.CompletedTask;
        }

        public IReadOnlyList<Contracts.EventRecord> Snapshot() => _records;

        public int Count => _records.Count;
    }
}
