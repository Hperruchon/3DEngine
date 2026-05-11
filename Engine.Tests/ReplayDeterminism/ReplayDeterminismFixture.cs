using Engine.Contracts;
using Engine.Core.Commands;

namespace Engine.Tests.ReplayDeterminism;

// Hand-authored fixture for the replay-determinism gate (TASK-0005, P4).
// Stability is what makes this a gate: the CommandIds, Echo strings, expected
// Document.Version, and expected event sequence are constants. A future change
// in CommandBus / Replay / Document / InMemoryEventSink that affects any of
// these values fails the gate test — the author either updates the fixture
// in the same PR or reverts the change.
internal static class ReplayDeterminismFixture
{
    public static readonly Guid AlphaCommandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid BetaCommandId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid GammaCommandId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static IReadOnlyList<Command> Commands { get; } = new Command[]
    {
        new NoOpCommand { CommandId = AlphaCommandId, Echo = "alpha" },
        new NoOpCommand { CommandId = BetaCommandId,  Echo = "beta" },
        new NoOpCommand { CommandId = GammaCommandId, Echo = "gamma" },
    };

    public const long ExpectedDocumentVersion = 3;

    public static IReadOnlyList<ExpectedEvent> ExpectedEvents { get; } = new[]
    {
        new ExpectedEvent(Seq: 1, Kind: "command.applied", CauseCommandId: AlphaCommandId),
        new ExpectedEvent(Seq: 2, Kind: "command.applied", CauseCommandId: BetaCommandId),
        new ExpectedEvent(Seq: 3, Kind: "command.applied", CauseCommandId: GammaCommandId),
    };

    public sealed record ExpectedEvent(long Seq, string Kind, Guid CauseCommandId);
}
