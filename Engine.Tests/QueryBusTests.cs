using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
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

    // TASK-0485 (finding E17 of the codebase review of 2026-09-30): a result of another type than the caller asks for has its
    // own code. Until TASK-0485 the bus gave E-QRY-UNKNOWN, the code of a query that no handler matches.
    [Fact]
    public async Task A_Result_Of_Another_Type_Gives_E_QRY_RESULT_TYPE()
    {
        var doc = new Document();
        var registry = new QueryRegistry();
        registry.Register(new TextHandler());
        var bus = new QueryBus(doc, registry);

        var wrong = await bus.Query<Aabb>(new TextQuery());
        var right = await bus.Query<string>(new TextQuery());

        Assert.Equal(DiagnosticCodes.QueryResultType, wrong.Error!.Code);
        Assert.Contains("String", wrong.Error.Message);
        Assert.Contains("Aabb", wrong.Error.Message);
        Assert.Null(right.Error);
        Assert.Equal("text", right.Result);
    }

    private sealed record TextQuery : Query
    {
        public override string Name => "TextQuery";
        public override int SchemaVersion => 1;
    }

    private sealed class TextHandler : IQueryHandler
    {
        public string QueryName => "TextQuery";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();
        public IReadOnlyDictionary<string, FieldSchema> Result { get; } = new Dictionary<string, FieldSchema>();

        public Task<QueryHandlerResult> Handle(Query query, Document document, IGeometryBackend backend, CancellationToken ct)
            => Task.FromResult(QueryHandlerResult.Success("text"));

        public Query Create(QueryInput input) => new TextQuery();
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
