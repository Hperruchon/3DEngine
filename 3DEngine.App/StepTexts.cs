using System.Text;
using Engine.Contracts;
using Engine.Core.Queries;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.Desktop;

// ADR-0127 items 1 and 2 (phase R18g, TASK-0354): the count of the entries of the log, and the text of the step that an Undo
// reverses, from the events of the session. Each applied command other than Undo and Redo is a step, as in the history of
// the command bus (ADR-0031 item 2): a new step clears the undone steps, an Undo moves the last step to the undone steps,
// and a Redo moves it back. The engine gives the name of a command only, so the text of a step is the text that the window
// expected for that command (Expect), else the name of the command in words. ADR-0129 item 3 (finding V177 of the codebase
// review of 2026-11-14, TASK-0356): the window expects a text for the identifier of its command, which each event of that
// command carries (CauseCommandId), so a command of another client with the same name does not take it. The texts are presentation (ADR-0007): they
// send nothing. The events come inside the commit of the session, on its thread, so each member takes the lock.
internal sealed class StepTexts
{
    private const string Applied = "command.applied";
    private const string Rejected = "command.rejected";
    private const string Cancelled = "command.cancelled";
    private const string UndoName = "Undo";
    private const string RedoName = "Redo";

    // The number of the expected texts that the class keeps; an older one goes first.
    internal const int ExpectedLimit = 32;

    private readonly Lock _lock = new();
    private readonly List<string> _done = [];
    private readonly List<string> _undone = [];
    private readonly List<(Guid Command, string Text)> _expected = [];

    // Finding V182 of the codebase review of 2026-11-14 (c) (TASK-0367): the count of the log right after the applied event of
    // each of the last commands, the newest last, at most ExpectedLimit of them.
    private readonly List<(Guid Command, int Count)> _counts = [];
    private bool _seeded;
    private int _log;

    // The count of the entries of the log, or null before the seed.
    public int? LogCount
    {
        get
        {
            lock (_lock)
                return _seeded ? _log : null;
        }
    }

    // The text of the step that the next Undo reverses, or null when no step is left to undo.
    public string? Undo
    {
        get
        {
            lock (_lock)
                return _done.Count > 0 ? _done[^1] : null;
        }
    }

    // The log of the Document at a read: the name of each entry in its order. The host subscribes Take inside the same
    // read of the session (EngineHost.Subscribe), so no event is lost and no event counts two times.
    public void Seed(IReadOnlyList<string> log)
    {
        ArgumentNullException.ThrowIfNull(log);
        lock (_lock)
        {
            (_log, _seeded) = (0, true);
            _counts.Clear();
            _done.Clear();
            _undone.Clear();
            foreach (var name in log)
                Count(name, Readable(name));
        }
    }

    // Finding V182: the count of the log right after the newest applied command of the list, or null when none of them
    // applied since the seed. The host gives it to the line after an accept, so a command of another client that commits
    // after the last command of the proposal, and before the frame that sees the end of the accept, takes Undo away.
    public int? CountAfter(IEnumerable<Guid> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        var wanted = commands.ToHashSet();
        lock (_lock)
        {
            for (var i = _counts.Count - 1; i >= 0; i--)
            {
                if (wanted.Contains(_counts[i].Command))
                    return _counts[i].Count;
            }
        }

        return null;
    }

    // The window gives the text of the command with this identifier, before it sends the command.
    public void Expect(Guid command, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        lock (_lock)
        {
            _expected.Add((command, text));
            if (_expected.Count > ExpectedLimit)
                _expected.RemoveAt(0);
        }
    }

    // The window sent its commands, so a text that no command took goes.
    public void Forget()
    {
        lock (_lock)
            _expected.Clear();
    }

    // Each event of the session after the seed, in the order of the commits.
    public void Take(EventRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        lock (_lock)
            Apply(record);
    }

    // "SetSketchConstraintValue" gives "Set sketch constraint value".
    internal static string Readable(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var text = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var letter = name[i];
            if (i > 0 && char.IsUpper(letter) && char.IsLower(name[i - 1]))
                text.Append(' ').Append(char.ToLowerInvariant(letter));
            else
                text.Append(letter);
        }

        return text.ToString();
    }

    // The text of a new value of a constraint, with its label in its sketch (ADR-0034 item 8): "C3 = 75", "C4 = 10, 20" for
    // two values, and "C1 = 45 deg"
    // for an angle, which holds its cosine and its sine (ADR-0033 item 3). Null when no live sketch holds the constraint.
    // Presentation: nothing of it enters the log (determinism rule 1 covers code that writes to the log only).
    internal static string? ValueText(IEnumerable<SketchInfo> sketches, Guid constraint, IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(sketches);
        ArgumentNullException.ThrowIfNull(values);
        foreach (var sketch in sketches)
        {
            if (sketch.Constraints.FirstOrDefault(candidate => candidate.Id == constraint) is not { } found)
                continue;
            var label = SketchText.Labels(sketch)[constraint];
            return found.Kind == "angle" && values is [var cosine, var sine]
                ? $"{label} = {SketchText.Short(Math.Atan2(sine, cosine) * 180 / Math.PI)} {DesktopUi.AngleUnit}"
                : values.Count > 0 ? $"{label} = {string.Join(", ", values.Select(SketchText.Short))}" : null;
        }

        return null;
    }

    // The text of a new distance of an extrude: "distance = 30", with the word of the lines of a proposal.
    internal static string DistanceText(double distance) => $"{ProposedValues.DistanceLabel} = {SketchText.Short(distance)}";

    // The identifier and the text of each command of a proposal whose value the frame names (ProposedValues.Named).
    internal static IEnumerable<(Guid Command, string Text)> ProposalTexts(ProposalCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        for (var i = 0; i < card.Commands.Count; i++)
        {
            if (card.Values is not { } values || i >= values.Count || values[i] is not { Label: { } label } value
                || card.CommandIds is not { } ids || i >= ids.Count)
                continue;
            var command = ids[i];
            yield return (command, $"{label} = {SketchText.Short(value.Shown ?? value.Number)}{(value.Unit is { } unit ? $" {unit}" : string.Empty)}");
        }
    }

    private void Apply(EventRecord record)
    {
        if (Name(record) is not { } name)
            return;
        if (record.Kind == Applied)
        {
            var at = _expected.FindIndex(expected => expected.Command == record.CauseCommandId);
            var text = at < 0 ? Readable(name) : _expected[at].Text;
            if (at >= 0)
                _expected.RemoveAt(at);
            Count(name, text);
            if (record.CauseCommandId is { } cause)
            {
                _counts.Add((cause, _log));
                if (_counts.Count > ExpectedLimit)
                    _counts.RemoveAt(0);
            }
        }
        else if (record.Kind is Rejected or Cancelled && _expected.FindIndex(expected => expected.Command == record.CauseCommandId) is var gone and >= 0)
        {
            _expected.RemoveAt(gone);
        }
    }

    // ADR-0031 item 2: an applied command adds one entry to the log and moves the history.
    private void Count(string name, string text)
    {
        _log++;
        switch (name)
        {
            case UndoName when _done.Count > 0:
                _undone.Add(_done[^1]);
                _done.RemoveAt(_done.Count - 1);
                break;
            case RedoName when _undone.Count > 0:
                _done.Add(_undone[^1]);
                _undone.RemoveAt(_undone.Count - 1);
                break;
            case UndoName or RedoName:
                break;
            default:
                _done.Add(text);
                _undone.Clear();
                break;
        }
    }

    private static string? Name(EventRecord record)
        => record.Payload is IReadOnlyDictionary<string, object?> payload && payload.TryGetValue("name", out var value) && value is string name ? name : null;
}
