using Engine.Contracts;
using Engine.Core.Commands;

namespace Engine.Tests.ReplayDeterminism;

// Hand-authored fixture for the replay-determinism gate (TASK-0005, P4).
// Extended for TASK-0011 (P7a) with a CreateBox step that exercises
// body-creation events and Document.Bodies projection determinism, and for
// TASK-0085 (R8a) with a rename and a hide of the box (ADR-0028 item 8), and for
// TASK-0100 (K2a) with a sketch, a point and a fixed constraint (ADR-0033).
//
// Stability is what makes this a gate: the CommandIds, parameter values,
// expected Document.Version, expected event sequence, and expected body
// state are constants. A future change in CommandBus / Replay / Document /
// InMemoryEventSink / backend wiring that affects any of these values fails
// the gate test — the author either updates the fixture in the same PR or
// reverts the change.
internal static class ReplayDeterminismFixture
{
    public static readonly Guid AlphaCommandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DeltaCommandId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid BetaCommandId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid GammaCommandId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid RenameCommandId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid HideCommandId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid SketchCommandId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid PointCommandId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid FixedCommandId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public static IReadOnlyList<Command> Commands { get; } = new Command[]
    {
        new NoOpCommand     { CommandId = AlphaCommandId, Echo = "alpha" },
        new CreateBoxCommand{ CommandId = DeltaCommandId, SizeX = 2.0, SizeY = 4.0, SizeZ = 8.0 },
        // Beta states the version that it saw: two commands are in the log
        // before it. ADR-0020 makes the version the count of applied commands,
        // so a replay gives the same answer to that check (TASK-0035). With the
        // old meaning the version was 3 here, and the replay rejected Beta.
        new NoOpCommand     { CommandId = BetaCommandId,  Echo = "beta", ExpectedDocumentVersion = 2 },
        new NoOpCommand     { CommandId = GammaCommandId, Echo = "gamma" },
        new RenameBodyCommand { CommandId = RenameCommandId, BodyId = DeltaCommandId, NewName = "Base plate" },
        new SetBodyVisibilityCommand { CommandId = HideCommandId, BodyId = DeltaCommandId, Visible = false },
        new CreateSketchCommand { CommandId = SketchCommandId, DatumId = Engine.Contracts.Sketch.Datums.XZ.Id, SketchName = "Profile" },
        new AddSketchPointCommand { CommandId = PointCommandId, SketchId = SketchCommandId, X = 0.1, Y = -0.0 },
        new AddSketchConstraintCommand { CommandId = FixedCommandId, SketchId = SketchCommandId, Kind = "fixed", First = PointCommandId, Value = 0.1, SecondValue = 0 },
    };

    // The version counts applied commands (ADR-0020): nine. Seq counts
    // events and is a separate counter.
    public const long ExpectedDocumentVersion = 9;

    // Seqs:
    // 1: alpha command.applied
    // 2: delta command.applied
    // 3: delta body.created
    // 4: beta  command.applied
    // 5: gamma command.applied
    // 6: rename command.applied
    // 7: rename body.changed
    // 8: hide  command.applied
    // 9: hide  body.changed
    // 10: sketch command.applied
    // 11: sketch sketch.created
    // 12: point command.applied
    // 13: point sketch.changed
    // 14: fixed command.applied
    // 15: fixed sketch.changed

    public static IReadOnlyList<ExpectedEvent> ExpectedEvents { get; } = new[]
    {
        new ExpectedEvent(Seq: 1, Kind: "command.applied", CauseCommandId: AlphaCommandId),
        new ExpectedEvent(Seq: 2, Kind: "command.applied", CauseCommandId: DeltaCommandId),
        new ExpectedEvent(Seq: 3, Kind: "body.created",    CauseCommandId: DeltaCommandId),
        new ExpectedEvent(Seq: 4, Kind: "command.applied", CauseCommandId: BetaCommandId),
        new ExpectedEvent(Seq: 5, Kind: "command.applied", CauseCommandId: GammaCommandId),
        new ExpectedEvent(Seq: 6, Kind: "command.applied", CauseCommandId: RenameCommandId),
        new ExpectedEvent(Seq: 7, Kind: "body.changed",    CauseCommandId: RenameCommandId),
        new ExpectedEvent(Seq: 8, Kind: "command.applied", CauseCommandId: HideCommandId),
        new ExpectedEvent(Seq: 9, Kind: "body.changed",    CauseCommandId: HideCommandId),
        new ExpectedEvent(Seq: 10, Kind: "command.applied", CauseCommandId: SketchCommandId),
        new ExpectedEvent(Seq: 11, Kind: "sketch.created", CauseCommandId: SketchCommandId),
        new ExpectedEvent(Seq: 12, Kind: "command.applied", CauseCommandId: PointCommandId),
        new ExpectedEvent(Seq: 13, Kind: "sketch.changed", CauseCommandId: PointCommandId),
        new ExpectedEvent(Seq: 14, Kind: "command.applied", CauseCommandId: FixedCommandId),
        new ExpectedEvent(Seq: 15, Kind: "sketch.changed", CauseCommandId: FixedCommandId),
    };

    // Per ADR-0012 §4: body handle equals CommandId for single-body create commands.
    public static IReadOnlyList<ExpectedBody> ExpectedBodies { get; } = new[]
    {
        new ExpectedBody(HandleId: DeltaCommandId, Kind: "Box", Name: "Base plate", Visible: false),
    };

    // ADR-0033 items 2 and 5: the sketch as text, with the bits of each position: the origin
    // of the frame first, then the point, then the constraint with its values.
    public static IReadOnlyList<string> ExpectedSketches { get; } = new[]
    {
        "77777777-7777-7777-7777-777777777777 00000000-0000-0000-0000-000000000002 Profile"
        + " | 77777777-7777-7777-7777-777777777777:0,0 88888888-8888-8888-8888-888888888888:4591870180066957722,-9223372036854775808"
        + " | 99999999-9999-9999-9999-999999999999:fixed:88888888-8888-8888-8888-888888888888:4591870180066957722,0",
    };

    public static string Text(Engine.Contracts.Sketch.SketchRecord sketch)
        => $"{sketch.Id} {sketch.DatumId} {sketch.Name}"
            + " | " + string.Join(" ", sketch.Points.Select(p => $"{p.Id}:{BitConverter.DoubleToInt64Bits(p.X)},{BitConverter.DoubleToInt64Bits(p.Y)}"))
            + " | " + string.Join(" ", sketch.Constraints.Select(c => $"{c.Id}:{c.Kind}:{string.Join(",", c.Items)}:{string.Join(",", c.Values.Select(BitConverter.DoubleToInt64Bits))}"));

    public sealed record ExpectedEvent(long Seq, string Kind, Guid CauseCommandId);
    public sealed record ExpectedBody(Guid HandleId, string Kind, string? Name, bool Visible);
}
