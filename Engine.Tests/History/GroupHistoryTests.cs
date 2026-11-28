using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Tests.History;

// ADR-0188 items 1 to 5 (phase P16, TASK-0463; Q2 A and Q3 A of the codebase review of 2026-11-23): the group of a command in
// the log, the unit of undo of a group, the refusal of a move with a group, an open and a replay that rebuild the same units,
// and the apply of a group in one serial section with one Undo of the group at a failure.
public sealed class GroupHistoryTests
{
    private static (DocumentSession Session, InMemoryEventSink Events) NewSession(params ICommandHandler[] extra)
    {
        var kit = EngineHosting.CreateDefault(new OperationsBackend());
        foreach (var handler in extra)
            kit.CommandRegistry.Register(handler);
        return (new DocumentSession(kit), kit.Events);
    }

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    // The live bodies as text, in a fixed order.
    private static Task<string[]> Live(DocumentSession session)
        => session.Read((document, _) => document.Bodies
            .Select(body => $"{body.Handle.Id} {body.Kind} {body.Name} {body.Visible} {body.GeometryKey}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray());

    private static Task<Command[]> Log(DocumentSession session) => session.Read((document, _) => document.Log.Select(entry => entry.Command).ToArray());

    // A group of five steps that create, move, consume, rename and hide bodies, so the reversal of one step needs the step after
    // it reversed first: the Translate consumes the box of the first step, and the Subtract consumes the moved box.
    private static Command[] Group(Guid group, Guid box, Guid moved, Guid cutter, Guid cut) =>
    [
        new CreateBoxCommand { CommandId = box, SizeX = 2, SizeY = 3, SizeZ = 1, GroupId = group },
        new TranslateCommand { CommandId = moved, BodyId = box, Dx = 1, Dy = 0, Dz = 0, GroupId = group },
        new CreateBoxCommand { CommandId = cutter, SizeX = 1, SizeY = 1, SizeZ = 5, GroupId = group },
        new SubtractCommand { CommandId = cut, MinuendBodyId = moved, SubtrahendBodyId = cutter, GroupId = group },
        new RenameBodyCommand { BodyId = cut, NewName = "Plate", GroupId = group },
    ];

    private static Command[] NewGroup(Guid group) => Group(group, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    // Item 3: one Undo reverses each step of the group in one commit, from the last step to the first, and gives the last step
    // in commandId. One Redo applies them again from the first to the last. A step of the person before the group is a unit of
    // its own.
    [Fact]
    public async Task One_Undo_Reverses_Each_Step_Of_A_Group_And_One_Redo_Applies_Them_Again()
    {
        var (session, events) = NewSession();
        await Applied(session, new CreateBoxCommand { SizeX = 9, SizeY = 9, SizeZ = 9 });
        var before = await Live(session);
        var group = NewGroup(Guid.NewGuid());
        foreach (var command in group)
            await Applied(session, command);
        var after = await Live(session);
        var seen = events.Snapshot().Count;

        var undo = await Applied(session, new UndoCommand());

        Assert.Equal(before, await Live(session));
        Assert.Equal(group[^1].CommandId, undo.Outputs.Values["commandId"]);
        var undoEvents = events.Snapshot().Skip(seen).Select(record => record.Kind).ToList();
        Assert.Equal(
            ["command.applied", "body.changed", "body.created", "body.created", "body.removed", "body.removed", "body.created", "body.removed", "body.removed"],
            undoEvents);

        await Applied(session, new RedoCommand());
        Assert.Equal(after, await Live(session));

        await Applied(session, new UndoCommand());
        Assert.Equal(before, await Live(session));
        await Applied(session, new UndoCommand());
        Assert.Empty(await Live(session));
        var nothing = await session.Apply(new UndoCommand());
        Assert.Equal(DiagnosticCodes.CommandNothingToUndo, nothing.Error?.Code);

        // Two groups one after the other are two units, and a step with no group after a group is a unit of its own.
        var (second, _) = NewSession();
        foreach (var command in NewGroup(Guid.NewGuid()))
            await Applied(second, command);
        var first = await Live(second);
        foreach (var command in NewGroup(Guid.NewGuid()))
            await Applied(second, command);
        var both = await Live(second);
        await Applied(second, new NoOpCommand { Echo = "a step" });
        await Applied(second, new UndoCommand());
        Assert.Equal(both, await Live(second));
        await Applied(second, new UndoCommand());
        Assert.Equal(first, await Live(second));
    }

    // Item 4: an Undo or a Redo with a group is refused before the history and the log.
    [Fact]
    public async Task An_Undo_Or_A_Redo_With_A_Group_Is_Refused()
    {
        var (session, _) = NewSession();
        foreach (var command in NewGroup(Guid.NewGuid()))
            await Applied(session, command);
        var live = await Live(session);

        foreach (Command move in new Command[] { new UndoCommand { GroupId = Guid.NewGuid() }, new RedoCommand { GroupId = Guid.NewGuid() } })
        {
            var refused = await session.Apply(move);
            Assert.Equal((CommandStatus.Rejected, DiagnosticCodes.CommandGroupMove), (refused.Status, refused.Error?.Code));
            Assert.Equal(5, refused.DocumentVersion);
        }

        Assert.Equal(live, await Live(session));
        Assert.Equal(5, (await Log(session)).Length);
    }

    // Item 2: an entry with no group has no member groupId, so its bytes are the bytes of before P16; an entry with a group
    // holds the member after expectedDocumentVersion, and a read gives the group back. A member that is not a string refuses
    // the open.
    [Fact]
    public void An_Entry_With_No_Group_Keeps_Its_Bytes_And_An_Entry_With_A_Group_Holds_It()
    {
        var registry = EngineHosting.CreateDefault(new OperationsBackend()).CommandRegistry;
        var id = new Guid("11111111-2222-3333-4444-555555555555");
        var group = new Guid("66666666-7777-8888-9999-000000000000");
        var backend = new BackendIdentity("test", "1");
        var parameters = new Dictionary<string, object?> { ["echo"] = "a" };

        var plain = EntryCodec.Entry(1, backend, new NoOpCommand { CommandId = id, Echo = "a" }, parameters, out _);
        Assert.Equal(
            "{\"version\":1,\"backend\":{\"name\":\"test\",\"version\":\"1\"},\"command\":{\"name\":\"NoOp\",\"schemaVersion\":1,"
            + "\"commandId\":\"11111111-2222-3333-4444-555555555555\",\"expectedDocumentVersion\":null,\"parameters\":{\"echo\":\"a\"}}}",
            plain);
        Assert.Null(EntryCodec.ReadEntry(plain!, registry).Command.GroupId);

        var grouped = EntryCodec.Entry(1, backend, new NoOpCommand { CommandId = id, Echo = "a", GroupId = group }, parameters, out _);
        Assert.Equal(
            "{\"version\":1,\"backend\":{\"name\":\"test\",\"version\":\"1\"},\"command\":{\"name\":\"NoOp\",\"schemaVersion\":1,"
            + "\"commandId\":\"11111111-2222-3333-4444-555555555555\",\"expectedDocumentVersion\":null,"
            + "\"groupId\":\"66666666-7777-8888-9999-000000000000\",\"parameters\":{\"echo\":\"a\"}}}",
            grouped);
        var read = (NoOpCommand)EntryCodec.ReadEntry(grouped!, registry).Command;
        Assert.Equal((id, group, "a"), (read.CommandId, read.GroupId, read.Echo));

        foreach (var bad in new[] { "null", "5", "\"not a uuid\"" })
        {
            var line = grouped!.Replace("\"66666666-7777-8888-9999-000000000000\"", bad, StringComparison.Ordinal);
            var refused = Assert.Throws<DocumentFileException>(() => EntryCodec.ReadEntry(line, registry));
            Assert.Equal(DiagnosticCodes.IoLoadFailed, refused.Code);
        }
    }

    // Item 3 and the open of ADR-0015 item 8: after a save and an open, one Undo still reverses the whole group, and one Redo
    // applies it again.
    [Fact]
    public async Task An_Open_Rebuilds_The_Same_Units()
    {
        var folder = Path.Combine(Path.GetTempPath(), "3de-group-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "group.3de");
            var (first, _) = NewSession();
            DocumentFile.Create(first, path);
            await Applied(first, new CreateBoxCommand { SizeX = 9, SizeY = 9, SizeZ = 9 });
            var before = await Live(first);
            foreach (var command in NewGroup(Guid.NewGuid()))
                await Applied(first, command);
            var after = await Live(first);
            first.Close();
            Assert.Equal(5, File.ReadAllLines(path).Count(line => line.Contains("\"groupId\":", StringComparison.Ordinal)));

            var (second, _) = NewSession();
            await DocumentFile.Open(second, path);
            Assert.Equal(after, await Live(second));
            await Applied(second, new UndoCommand());
            Assert.Equal(before, await Live(second));
            await Applied(second, new RedoCommand());
            Assert.Equal(after, await Live(second));
            await Applied(second, new UndoCommand());
            second.Close();

            // The Undo of the second session is in the file, so a third open is at the state before the group, and its next
            // Undo reverses the step of the person.
            var (third, _) = NewSession();
            await DocumentFile.Open(third, path);
            Assert.Equal(before, await Live(third));
            await Applied(third, new UndoCommand());
            Assert.Empty(await Live(third));
            third.Close();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // ADR-0190 (TASK-0473, finding E242 of the codebase review of 2026-11-25): the reversal of a failed group is an Undo with the
    // group, and it puts nothing on the redo list. A Redo after it is refused, and an Undo reverses the step before the group.
    // The walks of the log, a replay and an open of the file give the same history. Until TASK-0473 the Redo applied the two
    // first commands of the group, a part of a proposal that no preview showed.
    [Fact]
    public async Task A_Redo_After_A_Failed_Group_Applies_Nothing()
    {
        var folder = Path.Combine(Path.GetTempPath(), "3de-reversal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "reversal.3de");
            var (session, _) = NewSession();
            DocumentFile.Create(session, path);
            await Applied(session, new CreateBoxCommand { SizeX = 9, SizeY = 9, SizeZ = 9 });
            var before = await Live(session);
            var group = Guid.NewGuid();
            var bad = NewGroup(group);
            bad[2] = new CreateBoxCommand { SizeX = -1, SizeY = 1, SizeZ = 1, GroupId = group };

            var failed = await session.ApplyGroup(bad, version: 1);

            Assert.Equal((2, CommandStatus.Applied), (failed.FailedIndex, failed.Undo?.Status));
            var log = await session.Read((document, _) => document.Log.ToArray());
            Assert.Equal(("Undo", group), (log[^1].Command.Name, log[^1].Command.GroupId));
            Assert.Equal(DiagnosticCodes.CommandNothingToRedo, (await session.Apply(new RedoCommand())).Error?.Code);
            Assert.Equal(before, await Live(session));

            Guid[] expected = [log[0].Command.CommandId];
            Assert.Equal(expected, SketchRebuild.InEffect(log).Order());
            Assert.Equal(expected, FeatureList.Build(log).InEffect.Order());
            var replay = await Replay.ReplayLog(log, EngineHosting.CreateDefault(new OperationsBackend()).CommandRegistry, new OperationsBackend());
            Assert.Equal(before, replay.Document.Bodies
                .Select(body => $"{body.Handle.Id} {body.Kind} {body.Name} {body.Visible} {body.GeometryKey}")
                .OrderBy(text => text, StringComparer.Ordinal));
            session.Close();

            var (opened, _) = NewSession();
            await DocumentFile.Open(opened, path);
            Assert.Equal(before, await Live(opened));
            Assert.Equal(DiagnosticCodes.CommandNothingToRedo, (await opened.Apply(new RedoCommand())).Error?.Code);
            await Applied(opened, new UndoCommand());
            Assert.Empty(await Live(opened));
            opened.Close();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // ADR-0190 item 2: an Undo with a group from a client is still refused before the history and the file, also with the group
    // of the last unit, so only the session marks a reversal.
    [Fact]
    public async Task An_Undo_With_A_Group_From_A_Client_Is_Still_Refused()
    {
        var (session, _) = NewSession();
        var group = Guid.NewGuid();
        foreach (var command in NewGroup(group))
            await Applied(session, command);

        var refused = await session.Apply(new UndoCommand { GroupId = group });

        Assert.Equal(DiagnosticCodes.CommandGroupMove, refused.Error?.Code);
        Assert.DoesNotContain(await Log(session), command => command is UndoCommand);
    }

    // Item 3 and ADR-0012: a log with groups, an Undo of a group, an Undo and a Redo replays on a new backend to the same
    // Document, and the three walks of the history (the bus, FeatureList and SketchRebuild) give the same steps in effect.
    [Fact]
    public async Task A_Log_With_Groups_And_An_Undo_Of_A_Group_Replays_To_The_Same_Document_And_History()
    {
        var (session, _) = NewSession();
        await Applied(session, new CreateBoxCommand { SizeX = 9, SizeY = 9, SizeZ = 9 });
        var undone = NewGroup(Guid.NewGuid());
        foreach (var command in undone)
            await Applied(session, command);
        await Applied(session, new UndoCommand());
        var kept = NewGroup(Guid.NewGuid());
        foreach (var command in kept)
            await Applied(session, command);
        await Applied(session, new UndoCommand());
        await Applied(session, new RedoCommand());
        await Applied(session, new NoOpCommand { Echo = "after" });
        var log = await session.Read((document, _) => document.Log.ToArray());

        var replay = await Replay.ReplayLog(log, EngineHosting.CreateDefault(new OperationsBackend()).CommandRegistry, new OperationsBackend());
        Assert.Equal(await Live(session), replay.Document.Bodies
            .Select(body => $"{body.Handle.Id} {body.Kind} {body.Name} {body.Visible} {body.GeometryKey}")
            .OrderBy(text => text, StringComparer.Ordinal));

        HashSet<Guid> expected = [log[0].Command.CommandId, .. kept.Select(command => command.CommandId), log[^1].Command.CommandId];
        Assert.Equal(expected.Order(), SketchRebuild.InEffect(log).Order());
        Assert.Equal(expected.Order(), FeatureList.Build(log).InEffect.Order());

        // The bus agrees: two Undo commands reverse the NoOp and then the whole kept group.
        var (replayed, _) = NewSession();
        foreach (var entry in log)
            await Applied(replayed, entry.Command);
        await Applied(replayed, new UndoCommand());
        await Applied(replayed, new UndoCommand());
        Assert.Equal(1, await replayed.Read((document, _) => document.Bodies.Count));
    }

    // Item 5: a group at another version applies nothing; a group whose command does not apply is reversed by one Undo that the
    // log holds; a command whose identifier the bus knows stops the group too; a group with a command of no group is refused.
    [Fact]
    public async Task A_Group_Applies_In_One_Section_At_Its_Version_And_A_Failure_Is_Reversed_By_One_Undo()
    {
        var (session, _) = NewSession();
        await Applied(session, new CreateBoxCommand { SizeX = 9, SizeY = 9, SizeZ = 9 });
        var before = await Live(session);
        var group = Guid.NewGuid();

        var stale = await session.ApplyGroup(NewGroup(group), version: 0);
        Assert.True(stale.Stale);
        Assert.Equal((1L, 0), (stale.Version, stale.Results.Count));
        Assert.Equal(before, await Live(session));

        var bad = NewGroup(group);
        bad[2] = new CreateBoxCommand { SizeX = -1, SizeY = 1, SizeZ = 1, GroupId = group };
        var failed = await session.ApplyGroup(bad, version: 1);
        Assert.False(failed.Stale);
        Assert.Equal((2, false), (failed.FailedIndex, failed.Known));
        Assert.Equal([CommandStatus.Applied, CommandStatus.Applied, CommandStatus.Rejected], failed.Results.Select(result => result.Status));
        Assert.Equal(CommandStatus.Applied, failed.Undo?.Status);
        Assert.Equal(before, await Live(session));
        Assert.Equal(["CreateBox", "CreateBox", "Translate", "Undo"], (await Log(session)).Select(command => command.Name));

        // The bus knows the identifier of the box of the first command now, so a group with it stops there.
        var known = NewGroup(group);
        known[1] = new CreateBoxCommand { CommandId = bad[0].CommandId, SizeX = 1, SizeY = 1, SizeZ = 1, GroupId = group };
        var again = await session.ApplyGroup(known, version: 4);
        Assert.Equal((1, true), (again.FailedIndex, again.Known));
        Assert.Single(again.Results);
        Assert.Equal(before, await Live(session));

        // The first command that does not apply gives no Undo when no step of the group applied.
        var first = NewGroup(group);
        first[0] = new CreateBoxCommand { SizeX = 0, SizeY = 1, SizeZ = 1, GroupId = group };
        var none = await session.ApplyGroup(first, version: null);
        Assert.Equal(0, none.FailedIndex);
        Assert.Null(none.Undo);

        var applied = await session.ApplyGroup(NewGroup(group), version: 6);
        Assert.Null(applied.FailedIndex);
        Assert.Equal(5, applied.Results.Count);
        await Applied(session, new UndoCommand());
        Assert.Equal(before, await Live(session));

        await Assert.ThrowsAsync<ArgumentException>(() => session.ApplyGroup([new NoOpCommand { Echo = "no group" }], version: null));
        await Assert.ThrowsAsync<ArgumentException>(() => session.ApplyGroup([new NoOpCommand { Echo = "a", GroupId = group }, new NoOpCommand { Echo = "b", GroupId = Guid.NewGuid() }], version: null));
    }

    // Item 5 (Q3 A): a handler that throws in the middle of a group gets the same Undo of the group, and its exception goes to
    // the caller; the Document is as it was.
    [Fact]
    public async Task A_Handler_That_Throws_In_A_Group_Gets_An_Undo_Of_The_Group_And_Its_Exception_Goes_Out()
    {
        var (session, _) = NewSession(new ThrowHandler());
        var before = await Live(session);
        var group = Guid.NewGuid();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ApplyGroup(
            [new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1, GroupId = group }, new ThrowCommand { GroupId = group }],
            version: 0));

        Assert.Equal("A fault of the backend.", thrown.Message);
        // TASK-0473 (finding E245 of the codebase review of 2026-11-25): the exception names the index of the command that threw.
        Assert.Equal(1, thrown.Data[DocumentSession.FailedIndexKey]);
        Assert.Equal(before, await Live(session));
        Assert.Equal(["CreateBox", "Undo"], (await Log(session)).Select(command => command.Name));
    }

    // Finding E250 of the codebase review of 2026-11-28 (TASK-0477): when the file refuses the Undo of the group after a handler
    // throws, the exception says that the steps before it stay applied (ReversedKey false); when the Undo applies, it says true.
    [Fact]
    public async Task A_Throw_In_A_Group_Whose_Undo_Does_Not_Apply_Says_That_The_Steps_Stay()
    {
        var (session, _) = NewSession(new ThrowHandler());
        session.Attach(Engine.Core.Persistence.DocumentJournal.ForTest(
            Path.Combine(Path.GetTempPath(), $"3de-group-{Guid.NewGuid():N}.3de"), Document.Millimetre, new FailingOnText("\"Undo\"")));
        var group = Guid.NewGuid();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ApplyGroup(
            [new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1, GroupId = group }, new ThrowCommand { GroupId = group }],
            version: 0));

        Assert.Equal("A fault of the backend.", thrown.Message);
        Assert.Equal(1, thrown.Data[DocumentSession.FailedIndexKey]);
        Assert.Equal(false, thrown.Data[DocumentSession.ReversedKey]);
        Assert.Equal(["CreateBox"], (await Log(session)).Select(command => command.Name));

        var (clean, _) = NewSession(new ThrowHandler());
        var other = await Assert.ThrowsAsync<InvalidOperationException>(() => clean.ApplyGroup(
            [new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1, GroupId = group }, new ThrowCommand { GroupId = group }],
            version: 0));
        Assert.Equal(true, other.Data[DocumentSession.ReversedKey]);
    }

    // A stream whose write of a line that holds the text fails, as a full disk does.
    private sealed class FailingOnText(string text) : MemoryStream
    {
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Check(buffer);
            base.Write(buffer.ToArray(), 0, buffer.Length);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Check(buffer.AsSpan(offset, count));
            base.Write(buffer, offset, count);
        }

        private void Check(ReadOnlySpan<byte> buffer)
        {
            if (System.Text.Encoding.UTF8.GetString(buffer).Contains(text, StringComparison.Ordinal))
                throw new IOException("The disk is full.");
        }
    }

    private sealed record ThrowCommand : Command
    {
        public override string Name => "TestThrow";
        public override int SchemaVersion => 1;
    }

    private sealed class ThrowHandler : ICommandHandler
    {
        public string CommandName => "TestThrow";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();
        public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } = new Dictionary<string, FieldSchema>();

        public Command Create(CommandInput input) => throw new NotSupportedException();

        public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
            => throw new InvalidOperationException("A fault of the backend.");
    }

    // A backend that records each body and moves nothing, enough for the history of the bus.
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

        public void Rotate(BodyHandle result, BodyHandle source, Rotation rotation) => _bodies.Add(result.Id);

        public void Subtract(BodyHandle result, BodyHandle minuend, BodyHandle subtrahend) => _bodies.Add(result.Id);

        public void Union(BodyHandle result, BodyHandle first, BodyHandle second) => _bodies.Add(result.Id);

        public void Intersect(BodyHandle result, BodyHandle first, BodyHandle second) => _bodies.Add(result.Id);
    }
}
