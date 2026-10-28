using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Queries;

namespace Engine.Tests.Commands;

// The name and the visibility of a body (ADR-0028, TASK-0085). ADR-0028 names this file in
// enforced-by. The commands RenameBody and SetBodyVisibility, the query GetBodyInfo, the
// rules of a name, the event body.changed and its place in a commit, the refusals of the
// bus, inheritance through Translate and Subtract, and the snapshot.
public class BodyNameAndVisibilityTests
{
    private static readonly CreateBoxCommand Box = new() { SizeX = 1, SizeY = 1, SizeZ = 1 };

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession(params ICommandHandler[] extra)
    {
        var kit = EngineHosting.CreateDefault(new OperationsBackend());
        foreach (var handler in extra)
            kit.CommandRegistry.Register(handler);
        return (new DocumentSession(kit), kit.Events);
    }

    private static async Task<Guid> NewBox(DocumentSession session)
    {
        var result = await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.True(result.Outputs.TryGet<Guid>("bodyId", out var id));
        return id;
    }

    private static async Task<BodyInfo> Info(DocumentSession session, Guid id)
    {
        var result = await session.Query<BodyInfo>(new GetBodyInfoQuery { BodyId = id });
        Assert.Null(result.Error);
        return result.Result!;
    }

    [Fact]
    public async Task A_Body_Has_No_Name_And_Is_Visible_Until_A_Command_Changes_It()
    {
        var (session, _) = NewSession();
        var id = await NewBox(session);

        Assert.Equal(new BodyInfo(id, "Box", null, true), await Info(session, id));
    }

    [Fact]
    public async Task RenameBody_Sets_The_Name_And_An_Empty_Name_Clears_It()
    {
        var (session, _) = NewSession();
        var id = await NewBox(session);

        Assert.Equal(CommandStatus.Applied, (await session.Apply(new RenameBodyCommand { BodyId = id, NewName = "Base plate" })).Status);
        Assert.Equal("Base plate", (await Info(session, id)).Name);

        Assert.Equal(CommandStatus.Applied, (await session.Apply(new RenameBodyCommand { BodyId = id, NewName = "" })).Status);
        Assert.Null((await Info(session, id)).Name);
    }

    // ADR-0028 item 4 (R8-Q1): visibility is not a filter of the engine.
    [Fact]
    public async Task A_Hidden_Body_Still_Works_In_Each_Query_And_Command()
    {
        var (session, _) = NewSession();
        var id = await NewBox(session);

        Assert.Equal(CommandStatus.Applied, (await session.Apply(new SetBodyVisibilityCommand { BodyId = id, Visible = false })).Status);

        Assert.False((await Info(session, id)).Visible);
        Assert.Null((await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = id })).Error);
        Assert.Equal(CommandStatus.Applied, (await session.Apply(new RenameBodyCommand { BodyId = id, NewName = "Hidden" })).Status);
        var move = await session.Apply(new TranslateCommand { BodyId = id, Dx = 1, Dy = 0, Dz = 0 });
        Assert.Equal(CommandStatus.Applied, move.Status);
    }

    // ADR-0028 item 3 (R8-Q4): no trim, no normalization, no culture rules, and one name for
    // two bodies.
    [Theory]
    [InlineData("a")]
    [InlineData("  padded  ")]
    [InlineData("Boîte à outils")]
    [InlineData("日本")]
    [InlineData(null)]
    public async Task A_Valid_Name_Is_Stored_Exactly(string? name)
    {
        name ??= new string('x', BodyNames.MaximumLength);
        var (session, _) = NewSession();
        var first = await NewBox(session);
        var second = await NewBox(session);

        foreach (var id in new[] { first, second })
            Assert.Equal(CommandStatus.Applied, (await session.Apply(new RenameBodyCommand { BodyId = id, NewName = name })).Status);

        Assert.Equal(name, (await Info(session, first)).Name);
        Assert.Equal(name, (await Info(session, second)).Name);
    }

    [Theory]
    [InlineData("tab\tinside")]
    [InlineData("line\nbreak")]
    [InlineData("\u0000")]
    [InlineData("delete\u007f")]
    [InlineData(null)]
    public async Task An_Invalid_Name_Is_Rejected_With_Its_Code_And_Changes_Nothing(string? name)
    {
        name ??= new string('x', BodyNames.MaximumLength + 1);
        var (session, _) = NewSession();
        var id = await NewBox(session);

        var result = await session.Apply(new RenameBodyCommand { BodyId = id, NewName = name });

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(DiagnosticCodes.ValBodyNameInvalid, result.Error!.Code);
        Assert.Null((await Info(session, id)).Name);
        Assert.Equal(1, await session.Read((document, _) => document.Version));
    }

    [Fact]
    public async Task An_Unknown_Or_Consumed_Body_Is_Not_Found()
    {
        var (session, _) = NewSession();
        var id = await NewBox(session);
        Assert.Equal(CommandStatus.Applied, (await session.Apply(new TranslateCommand { BodyId = id, Dx = 1, Dy = 0, Dz = 0 })).Status);

        foreach (var gone in new[] { id, Guid.NewGuid() })
        {
            Assert.Equal(DiagnosticCodes.GeomBodyNotFound, (await session.Apply(new RenameBodyCommand { BodyId = gone, NewName = "x" })).Error!.Code);
            Assert.Equal(DiagnosticCodes.GeomBodyNotFound, (await session.Apply(new SetBodyVisibilityCommand { BodyId = gone, Visible = false })).Error!.Code);
            Assert.Equal(DiagnosticCodes.GeomBodyNotFound, (await session.Query<BodyInfo>(new GetBodyInfoQuery { BodyId = gone })).Error!.Code);
        }
    }

    // ADR-0028 items 2 and 7: each applied change gives command.applied and then body.changed
    // with the whole new state, also when the value does not change.
    [Fact]
    public async Task Each_Change_Gives_One_Body_Changed_Event_Also_With_The_Same_Value()
    {
        var (session, events) = NewSession();
        var id = await NewBox(session);
        var before = events.Snapshot().Count;

        await session.Apply(new RenameBodyCommand { BodyId = id, NewName = "A" });
        await session.Apply(new RenameBodyCommand { BodyId = id, NewName = "A" });
        await session.Apply(new SetBodyVisibilityCommand { BodyId = id, Visible = true });

        var later = events.Snapshot().Skip(before).ToArray();
        Assert.Equal(
            ["command.applied", "body.changed", "command.applied", "body.changed", "command.applied", "body.changed"],
            later.Select(record => record.Kind));
        var payloads = later.Where(record => record.Kind == "body.changed")
            .Select(record => (IReadOnlyDictionary<string, object?>)record.Payload!)
            .ToArray();
        Assert.All(payloads, payload => Assert.Equal(id, payload["bodyId"]));
        Assert.All(payloads, payload => Assert.Equal("A", payload["name"]));
        Assert.All(payloads, payload => Assert.Equal(true, payload["visible"]));
        Assert.Equal(4, await session.Read((document, _) => document.Version));
    }

    // ADR-0021 item 5 and ADR-0028 item 7: command.applied, each body.created, each
    // body.consumed, each body.changed.
    [Fact]
    public async Task The_Events_Of_One_Commit_Come_In_Their_Order()
    {
        var created = new BodyHandle(Guid.NewGuid());
        var (session, events) = NewSession(new ListsHandler(created));
        var consumed = await NewBox(session);
        var changed = await NewBox(session);
        var before = events.Snapshot().Count;

        Assert.Equal(CommandStatus.Applied, (await session.Apply(new ListsCommand { Consumed = consumed, Changed = changed })).Status);

        Assert.Equal(
            ["command.applied", "body.created", "body.consumed", "body.changed"],
            events.Snapshot().Skip(before).Select(record => record.Kind));
        Assert.Equal("Changed", (await Info(session, changed)).Name);
    }

    // ADR-0028 item 6: a list that breaks a rule is a defect of the handler, so the bus throws
    // before the commit, and the Document does not change.
    [Theory]
    [InlineData("not live")]
    [InlineData("consumed by the same command")]
    [InlineData("created by the same command")]
    [InlineData("two times")]
    [InlineData("another kind")]
    public async Task The_Bus_Refuses_A_Wrong_Changed_List_Before_It_Changes_The_Document(string fault)
    {
        var created = new BodyHandle(Guid.NewGuid());
        var (session, _) = NewSession(new ListsHandler(created, fault));
        var live = await NewBox(session);
        var spare = await NewBox(session);
        var before = await session.Read((document, _) => (document.Version, document.Bodies.Select(body => body.Name).ToArray()));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.Apply(new ListsCommand { Consumed = spare, Changed = live }));

        Assert.Contains("ADR-0028", error.Message, StringComparison.Ordinal);
        var after = await session.Read((document, _) => (document.Version, document.Bodies.Select(body => body.Name).ToArray()));
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.Item2, after.Item2);
    }

    // ADR-0028 item 5 (R8-Q5).
    [Fact]
    public async Task The_Result_Of_Translate_And_Subtract_Keeps_The_Name_And_Visibility_Of_Its_Source()
    {
        var (session, _) = NewSession();
        var source = await NewBox(session);
        var subtrahend = await NewBox(session);
        await session.Apply(new RenameBodyCommand { BodyId = source, NewName = "Plate" });
        await session.Apply(new SetBodyVisibilityCommand { BodyId = source, Visible = false });
        await session.Apply(new RenameBodyCommand { BodyId = subtrahend, NewName = "Tool" });

        var moved = await session.Apply(new TranslateCommand { BodyId = source, Dx = 1, Dy = 0, Dz = 0 });
        Assert.True(moved.Outputs.TryGet<Guid>("bodyId", out var movedId));
        Assert.Equal(new BodyInfo(movedId, "Solid", "Plate", false), await Info(session, movedId));

        var cut = await session.Apply(new SubtractCommand { MinuendBodyId = movedId, SubtrahendBodyId = subtrahend });
        Assert.True(cut.Outputs.TryGet<Guid>("bodyId", out var cutId));
        Assert.Equal(new BodyInfo(cutId, "Solid", "Plate", false), await Info(session, cutId));
    }

    // ADR-0028 item 7: body.created and each body of the snapshot give the name and the
    // visibility.
    [Fact]
    public async Task Body_Created_And_The_Snapshot_Give_The_Name_And_The_Visibility()
    {
        var (session, events) = NewSession();
        var id = await NewBox(session);
        await session.Apply(new RenameBodyCommand { BodyId = id, NewName = "Plate" });
        await session.Apply(new SetBodyVisibilityCommand { BodyId = id, Visible = false });

        var created = (IReadOnlyDictionary<string, object?>)events.Snapshot().First(record => record.Kind == "body.created").Payload!;
        Assert.Null(created["name"]);
        Assert.Equal(true, created["visible"]);

        var snapshot = await session.Read((document, _) => SnapshotProjector.Project(document, seq: 0));
        Assert.Equal(new SnapshotBodyEntry(id, "Box", "Plate", false), Assert.Single(snapshot.Bodies));
    }

    // A backend that stores each body and runs translate and subtract, with no geometry.
    private sealed class OperationsBackend : IGeometryBackend, IMeshOps, IGeometryQuery, ITransformOps, IBooleanOps
    {
        private readonly HashSet<Guid> _bodies = [];

        public BackendCapabilities Capabilities
            => BackendCapabilities.Mesh | BackendCapabilities.Query | BackendCapabilities.Transform | BackendCapabilities.Booleans;

        public T? TryGet<T>() where T : class => this as T;

        public void CreateBox(BodyHandle handle, BoxParameters parameters) => _bodies.Add(handle.Id);

        public Aabb GetBoundingBox(BodyHandle handle) => new(0, 0, 0, 1, 1, 1);

        public void Translate(BodyHandle result, BodyHandle source, double dx, double dy, double dz) => _bodies.Add(result.Id);

        public void Subtract(BodyHandle result, BodyHandle minuend, BodyHandle subtrahend) => _bodies.Add(result.Id);
    }

    private sealed record ListsCommand : Command
    {
        public override string Name => "TestBodyLists";
        public override int SchemaVersion => 1;

        public required Guid Consumed { get; init; }
        public required Guid Changed { get; init; }
    }

    // Creates one body, consumes one and changes one; a fault breaks the changed list.
    private sealed class ListsHandler(BodyHandle created, string? fault = null) : ICommandHandler
    {
        public string CommandName => "TestBodyLists";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();
        public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } = new Dictionary<string, FieldSchema>();

        public Command Create(CommandInput input) => throw new NotSupportedException();

        public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
        {
            var lists = (ListsCommand)command;
            var live = document.Bodies.Single(body => body.Handle.Id == lists.Changed);
            var renamed = live with { Name = "Changed" };
            BodyRecord[] changed = fault switch
            {
                "not live" => [new BodyRecord(new BodyHandle(Guid.NewGuid()), "Box", "Changed")],
                "consumed by the same command" => [document.Bodies.Single(body => body.Handle.Id == lists.Consumed) with { Name = "Changed" }],
                "created by the same command" => [new BodyRecord(created, "Box", "Changed")],
                "two times" => [renamed, renamed],
                "another kind" => [renamed with { Kind = "Solid" }],
                _ => [renamed],
            };

            return Task.FromResult(CommandHandlerResult.Success(
                Engine.Contracts.Outputs.Empty,
                createdBodies: [new BodyRecord(created, "Box")],
                consumedBodies: [new BodyHandle(lists.Consumed)],
                changedBodies: changed));
        }
    }
}
