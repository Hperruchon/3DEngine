using Engine.Contracts;
using Engine.Core.Commands;

namespace Engine.Tests.ReplayDeterminism;

// Hand-authored fixture for the replay-determinism gate (TASK-0005, P4).
// Extended for TASK-0011 (P7a) with a CreateBox step that exercises
// body-creation events and Document.Bodies projection determinism.
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

    public static IReadOnlyList<Command> Commands { get; } = new Command[]
    {
        new NoOpCommand     { CommandId = AlphaCommandId, Echo = "alpha" },
        new CreateBoxCommand{ CommandId = DeltaCommandId, SizeX = 2.0, SizeY = 4.0, SizeZ = 8.0 },
        new NoOpCommand     { CommandId = BetaCommandId,  Echo = "beta" },
        new NoOpCommand     { CommandId = GammaCommandId, Echo = "gamma" },
    };

    // Seqs:
    // 1: alpha command.applied
    // 2: delta command.applied
    // 3: delta body.created
    // 4: beta  command.applied
    // 5: gamma command.applied
    public const long ExpectedDocumentVersion = 5;

    public static IReadOnlyList<ExpectedEvent> ExpectedEvents { get; } = new[]
    {
        new ExpectedEvent(Seq: 1, Kind: "command.applied", CauseCommandId: AlphaCommandId),
        new ExpectedEvent(Seq: 2, Kind: "command.applied", CauseCommandId: DeltaCommandId),
        new ExpectedEvent(Seq: 3, Kind: "body.created",    CauseCommandId: DeltaCommandId),
        new ExpectedEvent(Seq: 4, Kind: "command.applied", CauseCommandId: BetaCommandId),
        new ExpectedEvent(Seq: 5, Kind: "command.applied", CauseCommandId: GammaCommandId),
    };

    // Per ADR-0012 §4: body handle equals CommandId for single-body create commands.
    public static IReadOnlyList<ExpectedBody> ExpectedBodies { get; } = new[]
    {
        new ExpectedBody(HandleId: DeltaCommandId, Kind: "Box"),
    };

    public sealed record ExpectedEvent(long Seq, string Kind, Guid CauseCommandId);
    public sealed record ExpectedBody(Guid HandleId, string Kind);
}
