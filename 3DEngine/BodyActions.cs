using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using ThreeDEngine.Core.Input;

namespace ThreeDEngine.Desktop;

// The selection of the desktop and the commands of a hide, a show all and a rename
// (ADR-0029 items 1, 5 and 6, TASK-0086). The selection is a state of the client (ADR-0007):
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
