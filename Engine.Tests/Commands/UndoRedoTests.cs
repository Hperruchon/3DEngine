using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;

namespace Engine.Tests.Commands;

// Undo and redo as commands of the log (ADR-0031, TASK-0093). ADR-0031 names this file in
// enforced-by. Each command type undone and redone, the redo list that a new step clears,
// the two rejections, the version and the log, the events and their order, a replay, and
// the refusal of a handler that gives a move and a list of bodies.
public class UndoRedoTests
{
    private static (DocumentSession Session, InMemoryEventSink Events) NewSession(params ICommandHandler[] extra)
    {
        var kit = EngineHosting.CreateDefault(new OperationsBackend());
        foreach (var handler in extra)
            kit.CommandRegistry.Register(handler);
        return (new DocumentSession(kit), kit.Events);
    }

    private static async Task<Guid> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>("bodyId", out var id) ? id : Guid.Empty;
    }

    // The live body set as text, in a fixed order.
    private static Task<string[]> Live(DocumentSession session)
        => session.Read((document, _) => document.Bodies
            .Select(body => $"{body.Handle.Id} {body.Kind} {body.Name} {body.Visible}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray());

    // One step of each command type of the catalog that changes bodies, and the live set
    // before the first step and after each step.
    private static async Task<List<string[]>> Scene(DocumentSession session)
    {
        var sets = new List<string[]> { await Live(session) };
        var plate = await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 3, SizeZ = 1 });
        sets.Add(await Live(session));
        var moved = await Applied(session, new TranslateCommand { BodyId = plate, Dx = 1, Dy = 0, Dz = 0 });
        sets.Add(await Live(session));
        var cutter = await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 5 });
        sets.Add(await Live(session));
        var cut = await Applied(session, new SubtractCommand { MinuendBodyId = moved, SubtrahendBodyId = cutter });
        sets.Add(await Live(session));
        await Applied(session, new RenameBodyCommand { BodyId = cut, NewName = "Plate" });
        sets.Add(await Live(session));
        await Applied(session, new SetBodyVisibilityCommand { BodyId = cut, Visible = false });
        sets.Add(await Live(session));
        await Applied(session, new NoOpCommand { Echo = "step" });
        sets.Add(await Live(session));
        return sets;
    }

    [Fact]
    public async Task Each_Undo_Gives_The_Live_Set_Before_Its_Step_And_Each_Redo_The_Set_After_It()
    {
        var (session, _) = NewSession();
        var sets = await Scene(session);
        var steps = sets.Count - 1;

        for (var step = steps; step > 0; step--)
        {
            await Applied(session, new UndoCommand());
            Assert.Equal(sets[step - 1], await Live(session));
        }

        for (var step = 1; step <= steps; step++)
        {
            await Applied(session, new RedoCommand());
            Assert.Equal(sets[step], await Live(session));
        }
    }

    [Fact]
    public async Task An_Undo_And_A_Redo_Go_Into_The_Log_Add_One_To_The_Version_And_Name_Their_Step()
    {
        var (session, _) = NewSession();
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        await Applied(session, box);

        var undo = await session.Apply(new UndoCommand());
        var redo = await session.Apply(new RedoCommand());

        Assert.Equal(2, undo.DocumentVersion);
        Assert.Equal(3, redo.DocumentVersion);
        Assert.Equal(box.CommandId, undo.Outputs.Values["commandId"]);
        Assert.Equal(box.CommandId, redo.Outputs.Values["commandId"]);
        Assert.Equal(["CreateBox", "Undo", "Redo"], await session.Read((document, _) => document.Log.Select(entry => entry.Command.Name).ToArray()));
    }

    [Fact]
    public async Task An_Undo_After_An_Undo_Reverses_The_Step_Before()
    {
        var (session, _) = NewSession();
        var first = await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 });

        await Applied(session, new UndoCommand());
        Assert.Single(await Live(session), text => text.StartsWith(first.ToString(), StringComparison.Ordinal));
        await Applied(session, new UndoCommand());

        Assert.Empty(await Live(session));
    }

    [Fact]
    public async Task A_New_Step_After_An_Undo_Clears_The_Redo_List()
    {
        var (session, _) = NewSession();
        await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        await Applied(session, new UndoCommand());
        await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 });

        var redo = await session.Apply(new RedoCommand());

        Assert.Equal(CommandStatus.Rejected, redo.Status);
        Assert.Equal(DiagnosticCodes.CommandNothingToRedo, redo.Error!.Code);
    }

    [Fact]
    public async Task A_Redo_After_A_Redo_Applies_The_Next_Undone_Step()
    {
        var (session, _) = NewSession();
        await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        var all = await Live(session);
        await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 });
        var both = await Live(session);
        await Applied(session, new UndoCommand());
        await Applied(session, new UndoCommand());

        await Applied(session, new RedoCommand());
        Assert.Equal(all, await Live(session));
        await Applied(session, new RedoCommand());
        Assert.Equal(both, await Live(session));
    }

    [Theory]
    [InlineData("Undo", "E-CMD-NOTHING-TO-UNDO")]
    [InlineData("Redo", "E-CMD-NOTHING-TO-REDO")]
    public async Task A_Move_With_Nothing_To_Move_Is_Rejected_And_Changes_Nothing(string name, string code)
    {
        var (session, _) = NewSession();
        await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        if (name == "Undo")
            await Applied(session, new UndoCommand());
        var before = await session.Read((document, _) => (document.Version, document.Log.Count));
        var live = await Live(session);

        var result = await session.Apply(name == "Undo" ? new UndoCommand() : new RedoCommand());

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(code, result.Error!.Code);
        Assert.Equal(before, await session.Read((document, _) => (document.Version, document.Log.Count)));
        Assert.Equal(live, await Live(session));
    }

    [Fact]
    public async Task An_Undo_With_A_Stale_Version_Is_Rejected_And_Keeps_The_Step()
    {
        var (session, _) = NewSession();
        await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        var live = await Live(session);

        var stale = await session.Apply(new UndoCommand { ExpectedDocumentVersion = 0 });

        Assert.Equal(DiagnosticCodes.CommandVersionStale, stale.Error!.Code);
        Assert.Equal(live, await Live(session));
        await Applied(session, new UndoCommand());
        Assert.Empty(await Live(session));
    }

    // ADR-0031 items 4 to 6: an undo of a subtract gives back its operands with their full
    // records, then removes its result; a redo gives the kinds of the first commit.
    [Fact]
    public async Task The_Events_Of_An_Undo_And_A_Redo_Of_A_Subtract_Come_In_The_Order_Of_A_Commit()
    {
        var (session, events) = NewSession();
        var a = await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 });
        var b = await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        await Applied(session, new RenameBodyCommand { BodyId = a, NewName = "Plate" });
        var cut = await Applied(session, new SubtractCommand { MinuendBodyId = a, SubtrahendBodyId = b });

        var before = events.Snapshot().Count;
        await Applied(session, new UndoCommand());
        var undone = events.Snapshot().Skip(before).ToList();
        before = events.Snapshot().Count;
        await Applied(session, new RedoCommand());
        var redone = events.Snapshot().Skip(before).ToList();

        Assert.Equal(["command.applied", "body.created", "body.created", "body.removed"], undone.Select(record => record.Kind));
        var plate = (IReadOnlyDictionary<string, object?>)undone[1].Payload!;
        Assert.Equal(a, plate["bodyId"]);
        Assert.Equal("Box", plate["kind"]);
        Assert.Equal("Plate", plate["name"]);
        Assert.Equal(true, plate["visible"]);
        Assert.Equal(cut, ((IReadOnlyDictionary<string, object?>)undone[3].Payload!)["bodyId"]);

        Assert.Equal(["command.applied", "body.created", "body.consumed", "body.consumed"], redone.Select(record => record.Kind));
        Assert.Equal(cut, ((IReadOnlyDictionary<string, object?>)redone[1].Payload!)["bodyId"]);
        var seqs = undone.Concat(redone).Select(record => record.Seq).ToList();
        Assert.Equal(Enumerable.Range(0, seqs.Count).Select(index => seqs[0] + index), seqs);
    }

    [Fact]
    public async Task An_Undo_Of_A_Rename_Gives_The_Name_Before_In_A_Changed_Event()
    {
        var (session, events) = NewSession();
        var box = await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        await Applied(session, new RenameBodyCommand { BodyId = box, NewName = "First" });
        await Applied(session, new RenameBodyCommand { BodyId = box, NewName = "Second" });

        var before = events.Snapshot().Count;
        await Applied(session, new UndoCommand());
        var undone = events.Snapshot().Skip(before).ToList();

        Assert.Equal(["command.applied", "body.changed"], undone.Select(record => record.Kind));
        Assert.Equal("First", ((IReadOnlyDictionary<string, object?>)undone[1].Payload!)["name"]);
    }

    // ADR-0031 item 3: the history depends only on the log, so a replay of a log with
    // moves gives the same live set, on each run.
    [Fact]
    public async Task A_Replay_Of_A_Log_With_Undo_And_Redo_Gives_The_Same_Live_Set()
    {
        var (session, _) = NewSession();
        await Scene(session);
        await Applied(session, new UndoCommand());
        await Applied(session, new UndoCommand());
        await Applied(session, new UndoCommand());
        await Applied(session, new RedoCommand());
        await Applied(session, new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 4 });
        await Applied(session, new UndoCommand());
        var log = await session.Read((document, _) => document.Log.ToList());
        var expected = await Live(session);

        foreach (var run in new[] { 1, 2 })
        {
            var registry = new CommandRegistry();
            foreach (var handler in HandlerCatalog.CommandHandlers())
                registry.Register(handler);
            var replay = await Replay.ReplayLog(log, registry, new OperationsBackend());

            Assert.Equal(expected, replay.Document.Bodies
                .Select(body => $"{body.Handle.Id} {body.Kind} {body.Name} {body.Visible}")
                .OrderBy(text => text, StringComparer.Ordinal));
        }
    }

    // ADR-0031 item 7: the bus computes the bodies of a move, so a result that gives a move
    // and a list is a defect of the handler, and the bus throws before the commit.
    [Fact]
    public async Task A_Handler_That_Gives_A_Move_And_A_List_Of_Bodies_Is_Refused()
    {
        var (session, _) = NewSession(new MoveWithListHandler());
        await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        var before = await session.Read((document, _) => document.Version);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => session.Apply(new MoveWithListCommand()));

        Assert.Contains("ADR-0031 item 7", error.Message, StringComparison.Ordinal);
        Assert.Equal(before, await session.Read((document, _) => document.Version));
    }

    // A backend that stores each body and runs translate and subtract, with no geometry.
    private sealed class OperationsBackend : IGeometryBackend, IMeshOps, IGeometryQuery, ITransformOps, IBooleanOps
    {
        private readonly HashSet<Guid> _bodies = [];

        public BackendIdentity Identity { get; } = new("test", "1");

        public BackendCapabilities Capabilities
            => BackendCapabilities.Mesh | BackendCapabilities.Query | BackendCapabilities.Transform | BackendCapabilities.Booleans;

        public T? TryGet<T>() where T : class => this as T;

        public void CreateBox(BodyHandle handle, BoxParameters parameters) => _bodies.Add(handle.Id);

        public Aabb GetBoundingBox(BodyHandle handle) => new(0, 0, 0, 1, 1, 1);

        public void Translate(BodyHandle result, BodyHandle source, double dx, double dy, double dz) => _bodies.Add(result.Id);

        public void Subtract(BodyHandle result, BodyHandle minuend, BodyHandle subtrahend) => _bodies.Add(result.Id);
    }

    private sealed record MoveWithListCommand : Command
    {
        public override string Name => "TestMoveWithList";
        public override int SchemaVersion => 1;
    }

    private sealed class MoveWithListHandler : ICommandHandler
    {
        public string CommandName => "TestMoveWithList";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();
        public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } = new Dictionary<string, FieldSchema>();

        public Command Create(CommandInput input) => throw new NotSupportedException();

        public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
            => Task.FromResult(CommandHandlerResult.Success(
                Engine.Contracts.Outputs.Empty,
                createdBodies: [new BodyRecord(new BodyHandle(command.CommandId), "Box")]) with { History = HistoryMove.Undo });
    }
}
