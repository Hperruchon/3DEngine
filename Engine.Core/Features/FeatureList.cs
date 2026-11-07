using System.Runtime.CompilerServices;
using Engine.Contracts;
using Engine.Core.Commands;

namespace Engine.Core.Features;

// ADR-0054 (phase P8d1, TASK-0173): the ordered feature list, the second of the three layers of P8d. It is a fold of
// the log: each entry applies one time, in the order of the log, so a command does not walk the whole log again. An
// Undo moves the last step in effect to the undone list and removes what it gave; a Redo applies it again; each other
// entry is a new step and clears the undone list (ADR-0031). The list holds no state of the Document and never changes
// it, so a replay and an open give the same list. The list of a Document advances at each call of Of, inside the
// serial boundary of the session (ADR-0019).
public sealed class FeatureList
{
    private static readonly ConditionalWeakTable<Document, FeatureList> Lists = new();

    private readonly Lock _gate = new();
    private int _applied;
    private readonly List<Command> _done = [];
    private readonly List<Command> _undone = [];
    private readonly HashSet<Guid> _logged = [];
    private readonly HashSet<Guid> _inEffect = [];
    private readonly Dictionary<Guid, int> _order = [];
    private readonly Dictionary<Guid, Command> _creators = [];
    private readonly Dictionary<Guid, Command> _consumers = [];
    private readonly Dictionary<Guid, List<double>> _distances = [];

    private FeatureList()
    {
    }

    // The list of the Document, advanced to the end of its log. The log of a Document only grows (ADR-0020).
    public static FeatureList Of(Document document)
    {
        var list = Lists.GetValue(document, _ => new FeatureList());
        list.Advance(document.Log);
        return list;
    }

    // A new list of a log, for a log that is not the log of a Document.
    public static FeatureList Build(IReadOnlyList<LogEntry> log)
    {
        var list = new FeatureList();
        list.Advance(log);
        return list;
    }

    // ADR-0054 item 2: the kinds of command that create an object, which are the rows of the list.
    public static bool IsFeature(Command command)
        => Creates(command) || command is CreateDatumCommand or CreateSketchCommand or PlaceImageCommand;

    // ADR-0039 item 1: the kinds of command that create a body.
    // ADR-0076 item 6: CreateCylinder is a creator too.
    // ADR-0083 item 1 (TASK-0258): ImportStl creates a mesh body, a feature with no input, as a box is.
    public static bool Creates(Command command)
        => command is CreateBoxCommand or CreateCylinderCommand or ImportStlCommand or ExtrudeCommand or TranslateCommand or SubtractCommand;

    // ADR-0021 item 3: the bodies that a command consumes, in the order of its fields.
    public static IReadOnlyList<Guid> Operands(Command command) => command switch
    {
        TranslateCommand translate => [translate.BodyId],
        SubtractCommand subtract => [subtract.MinuendBodyId, subtract.SubtrahendBodyId],
        _ => [],
    };

    // The identifier of each command whose step is in effect.
    public IReadOnlySet<Guid> InEffect => _inEffect;

    // The commands whose step is in effect, in the order of the log.
    public IReadOnlyList<Command> Steps => _done;

    // True when the log holds a command with the identifier, in effect or not.
    public bool Holds(Guid commandId) => _logged.Contains(commandId);

    // The command in effect that creates the handle, or null.
    public Command? Creator(Guid handle) => _creators.GetValueOrDefault(handle);

    // The command in effect that consumes the handle, or null.
    public Command? Consumer(Guid handle) => _consumers.GetValueOrDefault(handle);

    // The place of a command among the steps in effect, for an order that a hash set does not give.
    public int Order(Guid commandId) => _order.GetValueOrDefault(commandId, int.MaxValue);

    // ADR-0049 item 2: the Extrude in effect with the identifier, live or consumed, or null.
    public ExtrudeCommand? Extrude(Guid extrudeId) => _creators.GetValueOrDefault(extrudeId) as ExtrudeCommand;

    // ADR-0049 item 3: the distance of the last SetExtrudeDistance in effect for the extrude, or else its own distance.
    public double Distance(ExtrudeCommand extrude)
        => _distances.TryGetValue(extrude.CommandId, out var stack) && stack.Count > 0 ? stack[^1] : extrude.Distance;

    // ADR-0049 items 3 and 7: the distance in effect of the Extrude in effect with the identifier, or null.
    public double? DistanceInEffect(Guid extrudeId) => Extrude(extrudeId) is { } extrude ? Distance(extrude) : null;

    private void Advance(IReadOnlyList<LogEntry> log)
    {
        lock (_gate)
        {
            for (; _applied < log.Count; _applied++)
            {
                var command = log[_applied].Command;
                _logged.Add(command.CommandId);
                switch (command)
                {
                    case UndoCommand when _done.Count > 0:
                        _undone.Add(_done[^1]);
                        Remove(_done[^1]);
                        break;
                    case RedoCommand when _undone.Count > 0:
                        var redone = _undone[^1];
                        _undone.RemoveAt(_undone.Count - 1);
                        Add(redone);
                        break;
                    case UndoCommand or RedoCommand:
                        break;
                    default:
                        Add(command);
                        _undone.Clear();
                        break;
                }
            }
        }
    }

    private void Add(Command command)
    {
        _order[command.CommandId] = _done.Count;
        _done.Add(command);
        _inEffect.Add(command.CommandId);
        if (Creates(command))
            _creators[command.CommandId] = command;
        foreach (var operand in Operands(command))
            _consumers[operand] = command;
        if (command is SetExtrudeDistanceCommand set)
        {
            if (!_distances.TryGetValue(set.ExtrudeId, out var stack))
                _distances[set.ExtrudeId] = stack = [];
            stack.Add(set.Distance);
        }
    }

    // The reverse of Add for the last step in effect. One handle has at most one consumer in effect (ADR-0021 item 1),
    // so the step held each consumer that it gave.
    private void Remove(Command command)
    {
        _done.RemoveAt(_done.Count - 1);
        _order.Remove(command.CommandId);
        _inEffect.Remove(command.CommandId);
        _creators.Remove(command.CommandId);
        foreach (var operand in Operands(command))
            _consumers.Remove(operand);
        if (command is SetExtrudeDistanceCommand set)
            _distances[set.ExtrudeId].RemoveAt(_distances[set.ExtrudeId].Count - 1);
    }
}
