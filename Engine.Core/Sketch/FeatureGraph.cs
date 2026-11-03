using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Core.Features;

namespace Engine.Core.Sketch;

// ADR-0039 item 1: the feature graph, a projection of the steps in effect of the log. It gives the
// command that creates each handle and the command that consumes it. A CreateBox, an Extrude, a
// Translate and a Subtract create the body whose handle is their command identifier; a Translate
// consumes its body, and a Subtract consumes its two operands (ADR-0021 item 3). The graph holds
// no state of its own and never changes the Document. ADR-0054 item 4: it is a view of the feature
// list, so the graph of a Document reads no entry of the log a second time.
public sealed class FeatureGraph
{
    private readonly FeatureList _list;

    private FeatureGraph(FeatureList list) => _list = list;

    public static FeatureGraph Of(Document document) => new(FeatureList.Of(document));

    public static FeatureGraph Of(IReadOnlyList<LogEntry> log) => new(FeatureList.Build(log));

    // The command in effect that creates the handle, or null.
    public Command? Creator(Guid handle) => _list.Creator(handle);

    // The command in effect that consumes the handle, or null.
    public Command? Consumer(Guid handle) => _list.Consumer(handle);

    // The place of a command among the steps in effect, for an order that a hash set does not give.
    public int Order(Guid commandId) => _list.Order(commandId);

    // The kinds of command that create a body, which the graph knows.
    public static bool Creates(Command command) => FeatureList.Creates(command);

    // The bodies that a command consumes, in the order of its fields.
    public static IReadOnlyList<Guid> Operands(Command command) => FeatureList.Operands(command);
}
