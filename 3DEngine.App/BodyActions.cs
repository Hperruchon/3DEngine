using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Picking;

namespace ThreeDEngine.Desktop;

// The selection of the desktop and the commands of a hide, a show all and a rename
// (ADR-0029 items 1, 5 and 6, TASK-0086), an undo and a redo (ADR-0031), and a new box and a
// move from the pad (ADR-0032), and a new distance of an extrude (ADR-0049). The selection is a
// state of the client (ADR-0007): one body or none. Each change of a body is a command of the
// session, the same command that an agent sends (ADR-0019 item 2), so the log records each one.
// The desktop calls this on its render loop, outside each commit.
internal sealed class BodyActions(DocumentSession session)
{
    private Guid? _selection;

    // The selected body, or null. A change of the body clears the selected face (ADR-0075 item 2).
    public Guid? Selection
    {
        get => _selection;
        private set
        {
            if (_selection != value)
                Face = null;
            _selection = value;
        }
    }

    // ADR-0075 item 2 (phase R12a, TASK-0236): the selected face of the selected body, by its name, or null.
    public DatumReference? Face { get; private set; }

    // Selects a body, or none. The row of a body in the panel gives the body, with no face.
    public void Select(Guid? body)
    {
        Selection = body;
        Face = null;
    }

    // Applies an intent of the selection or of the visibility, with the pick of a point of the
    // window. It returns false for each other intent, which the host applies to the camera.
    public bool Apply(Intent intent, Func<double, double, PickHit?> pick, IReadOnlyList<LiveBody> live)
    {
        ArgumentNullException.ThrowIfNull(pick);
        ArgumentNullException.ThrowIfNull(live);

        switch (intent)
        {
            // ADR-0075 item 2: a tap on the selected body selects the face under the point, and a tap on another body or
            // on no body selects that body or none. A tap on a part of the body that GetFaceAt does not name, as a curved
            // side, gives the body again.
            case SelectIntent select:
            {
                var hit = pick(select.X, select.Y);
                if (hit is { } on && on.Id == Selection)
                    Face = FaceAt(on);
                else
                    Selection = hit?.Id;
                return true;
            }

            // The key Escape goes back from the face to its body.
            case CancelIntent when Face is not null:
                Face = null;
                return true;

            case HideIntent hide:
                var target = hide.X is { } x && hide.Y is { } y ? pick(x, y)?.Id : Selection;
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

    // Phase K5b (ADR-0076, TASK-0244): a cylinder of the radius and the height of the pad. It returns null when the session
    // applied it, or the rejection for the pad.
    public string? CreateCylinder(double radius, double height)
        => Rejection(session.Apply(new CreateCylinderCommand { Radius = radius, Height = height }).GetAwaiter().GetResult());

    // ADR-0032 item 4 (Q3 A): a move by the offsets of the pad. Translate consumes the body and
    // makes a new one, and the new body becomes the selection.
    public string? Move(Guid body, double dx, double dy, double dz)
    {
        var result = session.Apply(new TranslateCommand { BodyId = body, Dx = dx, Dy = dy, Dz = dz }).GetAwaiter().GetResult();
        if (result.Status == CommandStatus.Applied && result.Outputs.TryGet<Guid>("bodyId", out var moved))
            Selection = moved;
        return Rejection(result);
    }

    // ADR-0075 item 3 (phase R12a, TASK-0236): the entry of GetFaces of the selected face, or null. A face whose name the
    // body no longer has, for example after a cut, or a body whose faces are not known, is no longer selected.
    public FaceEntry? SelectedFace()
    {
        if (Selection is not { } body || Face is not { } face)
            return null;
        var entry = session.Query<FacesInfo>(new GetFacesQuery { BodyId = body }).GetAwaiter().GetResult().Result?
            .Faces.FirstOrDefault(candidate => candidate.Reference == face);
        if (entry is null)
            Face = null;
        return entry;
    }

    // ADR-0073 item 2 (phase R14e3, TASK-0227): a cut of the body with the tool. Subtract consumes both bodies and makes a
    // new one, and the new body becomes the selection, as for a move. It returns null when the session applied it, or the
    // rejection for the line of the panel.
    public string? Cut(Guid body, Guid tool)
    {
        var result = session.Apply(new SubtractCommand { MinuendBodyId = body, SubtrahendBodyId = tool }).GetAwaiter().GetResult();
        if (result.Status == CommandStatus.Applied && result.Outputs.TryGet<Guid>("bodyId", out var cut))
            Selection = cut;
        return Rejection(result);
    }

    // ADR-0049 items 7 and 8: the distance in effect of the extrude whose handle is the body, from
    // GetBodyInfo, or null for another body. A new distance from the pad: the body keeps its
    // handle, so the selection stays.
    // The name of the face under a hit, from GetFaceAt (ADR-0053 item 1), or null.
    private DatumReference? FaceAt(PickHit hit)
        => session.Query<FaceInfo>(new GetFaceAtQuery
        {
            BodyId = hit.Id,
            X = hit.Point.X,
            Y = hit.Point.Y,
            Z = hit.Point.Z,
            Nx = hit.Normal.X,
            Ny = hit.Normal.Y,
            Nz = hit.Normal.Z,
        }).GetAwaiter().GetResult().Result?.Reference;

    public double? Distance(Guid body)
        => session.Query<BodyInfo>(new GetBodyInfoQuery { BodyId = body }).GetAwaiter().GetResult().Result?.Distance;

    public string? SetDistance(Guid body, double distance)
        => Rejection(session.Apply(new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = distance }).GetAwaiter().GetResult());

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
