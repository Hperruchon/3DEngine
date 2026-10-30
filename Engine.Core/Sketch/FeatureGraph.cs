using Engine.Contracts;
using Engine.Core.Commands;

namespace Engine.Core.Sketch;

// ADR-0039 item 1: the feature graph, a projection of the steps in effect of the log. It gives the
// command that creates each handle and the command that consumes it. A CreateBox, an Extrude, a
// Translate and a Subtract create the body whose handle is their command identifier; a Translate
// consumes its body, and a Subtract consumes its two operands (ADR-0021 item 3). The graph holds
// no state of its own and never changes the Document.
public sealed class FeatureGraph
{
    private readonly Dictionary<Guid, Command> _creators = new();
    private readonly Dictionary<Guid, Command> _consumers = new();
    private readonly Dictionary<Guid, int> _order = new();

    private FeatureGraph()
    {
    }

    public static FeatureGraph Of(IReadOnlyList<LogEntry> log)
    {
        var graph = new FeatureGraph();
        var inEffect = SketchRebuild.InEffect(log);
        var index = 0;
        foreach (var entry in log)
        {
            var command = entry.Command;
            if (!inEffect.Contains(command.CommandId))
                continue;

            graph._order[command.CommandId] = index++;
            if (Creates(command))
                graph._creators[command.CommandId] = command;
            foreach (var operand in Operands(command))
                graph._consumers[operand] = command;
        }

        return graph;
    }

    // The command in effect that creates the handle, or null.
    public Command? Creator(Guid handle) => _creators.GetValueOrDefault(handle);

    // The command in effect that consumes the handle, or null.
    public Command? Consumer(Guid handle) => _consumers.GetValueOrDefault(handle);

    // The place of a command among the steps in effect, for an order that a hash set does not give.
    public int Order(Guid commandId) => _order.GetValueOrDefault(commandId, int.MaxValue);

    // The kinds of command that create a body, which the graph knows.
    public static bool Creates(Command command) => command is CreateBoxCommand or ExtrudeCommand or TranslateCommand or SubtractCommand;

    // The bodies that a command consumes, in the order of its fields.
    public static IReadOnlyList<Guid> Operands(Command command) => command switch
    {
        TranslateCommand translate => [translate.BodyId],
        SubtractCommand subtract => [subtract.MinuendBodyId, subtract.SubtrahendBodyId],
        _ => [],
    };
}
