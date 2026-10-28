using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using ThreeDEngine.Core.Input;

namespace ThreeDEngine.Desktop;

// The selection of the desktop and the commands of a hide, a show all and a rename
// (ADR-0029 items 1, 5 and 6, TASK-0086), an undo and a redo (ADR-0031), and a new box and a
// move from the pad (ADR-0032). The selection is a state of the client (ADR-0007):
// one body or none. Each change of a body is a command of the session, the same command that
// an agent sends (ADR-0019 item 2), so the log records each one. The desktop calls this on
// its render loop, outside each commit.
internal sealed class BodyActions(DocumentSession session)
{
    // The selected body, or null.
    public Guid? Selection { get; private set; }

    // Selects a body, or none.
    public void Select(Guid? body) => Selection = body;

    // Applies an intent of the selection or of the visibility, with the pick of a point of the
    // window. It returns false for each other intent, which the host applies to the camera.
    public bool Apply(Intent intent, Func<double, double, Guid?> pick, IReadOnlyList<LiveBody> live)
    {
        ArgumentNullException.ThrowIfNull(pick);
        ArgumentNullException.ThrowIfNull(live);

        switch (intent)
        {
            case SelectIntent select:
                Selection = pick(select.X, select.Y);
                return true;

            case HideIntent hide:
                var target = hide.X is { } x && hide.Y is { } y ? pick(x, y) : Selection;
                if (target is { } body)
                    SetVisible(body, false);
                return true;

            case ShowAllIntent:
                ShowAll(live);
                return true;

            default:
                return false;
        }
    }

    // Shows each hidden live body, with one command for each.
    public void ShowAll(IReadOnlyList<LiveBody> live)
    {
        foreach (var body in live.Where(body => !body.Visible).ToArray())
            SetVisible(body.Id, true);
    }

    // A hidden body cannot stay selected, because the desktop does not draw it.
    public void SetVisible(Guid body, bool visible)
    {
        Report(session.Apply(new SetBodyVisibilityCommand { BodyId = body, Visible = visible }).GetAwaiter().GetResult());
        if (!visible && Selection == body)
            Selection = null;
    }

    public void Rename(Guid body, string name)
        => Report(session.Apply(new RenameBodyCommand { BodyId = body, NewName = name }).GetAwaiter().GetResult());

    // ADR-0031 item 11: an Undo or a Redo of the shared history. It returns null when the
    // session applied it, or the rejection for the line of the panel. Forget then drops a
    // selected body that the move removed.
    public string? Undo() => Rejection(session.Apply(new UndoCommand()).GetAwaiter().GetResult());

    public string? Redo() => Rejection(session.Apply(new RedoCommand()).GetAwaiter().GetResult());

    // ADR-0032 item 4: a box of the sizes of the pad. It returns null when the session applied
    // it, or the rejection for the pad.
    public string? CreateBox(double x, double y, double z)
        => Rejection(session.Apply(new CreateBoxCommand { SizeX = x, SizeY = y, SizeZ = z }).GetAwaiter().GetResult());

    // ADR-0032 item 4 (Q3 A): a move by the offsets of the pad. Translate consumes the body and
    // makes a new one, and the new body becomes the selection.
    public string? Move(Guid body, double dx, double dy, double dz)
    {
        var result = session.Apply(new TranslateCommand { BodyId = body, Dx = dx, Dy = dy, Dz = dz }).GetAwaiter().GetResult();
        if (result.Status == CommandStatus.Applied && result.Outputs.TryGet<Guid>("bodyId", out var moved))
            Selection = moved;
        return Rejection(result);
    }

    private static string? Rejection(CommandResult result)
    {
        Report(result);
        return result.Status == CommandStatus.Applied ? null : $"{result.Error?.Code}: {result.Error?.Message}";
    }

    // A selected body that left the session, for example one that an agent consumed, is no
    // longer selected.
    public void Forget(IReadOnlyList<LiveBody> live)
    {
        if (Selection is { } selected && !live.Any(body => body.Id == selected && body.Visible))
            Selection = null;
    }

    private static void Report(CommandResult result)
    {
        if (result.Status != CommandStatus.Applied)
            Console.WriteLine($"[WARN] The command {result.CommandName} was not applied: {result.Error?.Code} {result.Error?.Message}");
    }
}
