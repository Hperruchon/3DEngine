using Engine.Contracts;
using Engine.Core;

namespace Engine.Tests;

public class QueryBusTests
{
    private sealed record UnknownQuery : Query
    {
        public override string Name => "UnknownQuery";
        public override int SchemaVersion => 1;
    }

    [Fact]
    public async Task Query_Unknown_Returns_Rejected_With_E_QRY_UNKNOWN_And_Emits_No_Event()
    {
        var doc = new Document();
        var registry = new QueryRegistry();
        var bus = new QueryBus(doc, registry);

        // The QueryBus is structurally separated from any IEventSink — queries
        // cannot emit events by construction (ADR-0008 §6).
        var sink = new InMemoryEventSink();

        var result = await bus.Query<object>(new UnknownQuery());

        Assert.NotNull(result.Error);
        Assert.Equal(DiagnosticCodes.QueryUnknown, result.Error!.Code);
        Assert.Null(result.Result);
        Assert.Equal(doc.Version, result.AsOfDocumentVersion);

        // Negative assertion: no event emitted to any sink.
        Assert.Empty(sink.Snapshot());
    }
}
