using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;

namespace Engine.Tests;

public class QueryBusTests
{
    private sealed record UnknownQuery : Query
    {
        public override string Name => "UnknownQuery";
        public override int SchemaVersion => 1;
    }

    [Fact]
    public async Task Query_Unknown_Returns_Rejected_With_E_QRY_UNKNOWN()
    {
        var doc = new Document();
        var registry = new QueryRegistry();
        var bus = new QueryBus(doc, registry);

        var result = await bus.Query<object>(new UnknownQuery());

        Assert.NotNull(result.Error);
        Assert.Equal(DiagnosticCodes.QueryUnknown, result.Error!.Code);
        Assert.Null(result.Result);
        Assert.Equal(doc.Version, result.AsOfDocumentVersion);
    }

    // ADR-0008 §6: a query emits no event. The QueryBus has no sink, so the test
    // runs each query through a session, which holds the sink of the engine, and
    // then reads that sink. The earlier form asserted on a new sink that no bus
    // received, which proved nothing (codebase review of 2026-09-30, section 6,
    // step 5).
    [Fact]
    public async Task A_Query_Through_A_Session_Emits_No_Event()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        var session = new DocumentSession(kit);
        await session.Apply(new NoOpCommand { Echo = "before" });
        var before = kit.Events.Snapshot();

        await session.Query<object>(new UnknownQuery());
        await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = Guid.NewGuid() });

        Assert.Single(before);
        Assert.Equal(before, kit.Events.Snapshot());
    }
}
