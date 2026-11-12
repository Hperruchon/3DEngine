using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop.Ui;

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
            // ADR-0101 item 5 (phase R17b, TASK-0315): a selection of another body, or of no body, ends the focus.
            if (value != Focus)
                Focus = null;
            _selection = value;
        }
    }

    // ADR-0101 item 1 (phase R17b, TASK-0315): the body of the focus, or null. It is a state of the client: no command, so the
    // log, Undo and the visibility of each body do not change. While a focus lasts, the selection is its body.
    public Guid? Focus { get; private set; }

    // ADR-0101 items 3 and 5: "Focus" starts a focus on the selected body, and "Focus" again, or the close mark, ends it. The
    // body stays selected.
    public void ToggleFocus() => Focus = Focus is null ? Selection : null;

    public void EndFocus() => Focus = null;

    private DatumReference? _face;

    // ADR-0075 item 2 (phase R12a, TASK-0236): the selected face of the selected body, by its name, or null. ADR-0077 item 3
    // (phase R12b, TASK-0245): a change of the face clears the selected edge.
    public DatumReference? Face
    {
        get => _face;
        private set
        {
            if (_face != value)
                Edge = null;
            _face = value;
        }
    }

    // ADR-0077 item 3: the selected edge of the selected face, by the pair of the names of its faces, or null.
    public (DatumReference First, DatumReference Second)? Edge { get; private set; }

    // Selects a body, or none. The row of a body in the panel gives the body, with no face.
    public void Select(Guid? body)
    {
        Selection = body;
        Face = null;
    }

    // ADR-0100 item 2 (phase R17a, TASK-0314): a tap on a segment of the path line selects that level. The document clears
    // the body, the face and the edge; the body clears the face and the edge; the face clears the edge.
    public void Up(PathLevel level)
    {
        switch (level)
        {
            case PathLevel.Document:
                Selection = null;
                break;
            case PathLevel.Body:
                Face = null;
                break;
            case PathLevel.Face:
                Edge = null;
                break;
        }
    }

    // Applies an intent of the selection or of the visibility, with the pick of a point of the
    // window. It returns false for each other intent, which the host applies to the camera.
    // ADR-0077 item 3: edgeAt gives the edge of the selected face nearest a point of the window, at most 12 pixels away, or
    // null; a host with no edges gives none.
    public bool Apply(Intent intent, Func<double, double, PickHit?> pick, IReadOnlyList<LiveBody> live,
        Func<double, double, (DatumReference First, DatumReference Second)?>? edgeAt = null)
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
                // ADR-0077 item 3: near an edge of the selected face, the tap selects the edge and keeps the face. Finding
                // V117 of the codebase review of 2026-11-06 (e) (TASK-0252): the host asks for the edge before the pick, so
                // that a tap within 12 pixels of an edge on the silhouette, outside the body, selects the edge too; until
                // TASK-0252 such a tap missed the body and cleared the selection.
                if (Face is not null && edgeAt?.Invoke(select.X, select.Y) is { } edge)
                {
                    Edge = edge;
                    return true;
                }

                var hit = pick(select.X, select.Y);
                if (hit is { } on && on.Id == Selection)
                {
                    Edge = null;
                    Face = FaceAt(on);
                }
                // ADR-0101 item 7: inside a focus, a tap on no body goes up to the level of the body, and the focus stays.
                else if (hit is null && Focus is not null)
                    Face = null;
                else
                    Selection = hit?.Id;
                return true;
            }

            // The key Escape goes back from the edge to its face, and from the face to its body (ADR-0077 item 3).
            // ADR-0100 item 3 (phase R17a, TASK-0314): and from the body to the document. Until TASK-0314 Escape stopped at the
            // body.
            case CancelIntent { ByKey: true } when Edge is not null:
                Edge = null;
                return true;
            case CancelIntent { ByKey: true } when Face is not null:
                Face = null;
                return true;
            // ADR-0101 item 6 (phase R17b, TASK-0315): at the level of the body, Escape ends the focus first and keeps the body.
            case CancelIntent { ByKey: true } when Focus is not null:
                Focus = null;
                return true;
            case CancelIntent { ByKey: true } when Selection is not null:
                Selection = null;
                return true;

            case HideIntent hide:
                var target = hide.X is { } x && hide.Y is { } y ? pick(x, y)?.Id : Selection;
                if (target is { } body)
                    SetVisible(body, false, live);
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
            SetVisible(body.Id, true, live);
    }

    // A hidden body cannot stay selected, because the desktop does not draw it. ADR-0124 item 1 (phase R20b2, TASK-0351):
    // the selection goes to a neighbour in the order of the list (Neighbour). Until TASK-0351 it went to none, and the list
    // of the bodies came back over the model.
    public void SetVisible(Guid body, bool visible, IReadOnlyList<LiveBody> live)
    {
        ArgumentNullException.ThrowIfNull(live);
        var result = session.Apply(new SetBodyVisibilityCommand { BodyId = body, Visible = visible }).GetAwaiter().GetResult();
        Report(result);
        if (!visible && Selection == body && result.Status == CommandStatus.Applied)
            Selection = Neighbour(body, live);
    }

    // ADR-0124 item 1: the next visible body after this body in the order of the list, else the nearest visible body before
    // it, else none.
    internal static Guid? Neighbour(Guid body, IReadOnlyList<LiveBody> live)
    {
        var at = live.Select(entry => entry.Id).ToList().IndexOf(body);
        if (at < 0)
            return null;
        var after = live.Skip(at + 1).FirstOrDefault(entry => entry.Visible && entry.Id != body);
        return after?.Id ?? live.Take(at).LastOrDefault(entry => entry.Visible && entry.Id != body)?.Id;
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

    // Phase K5e (ADR-0089 item 5, TASK-0298): a cone, a sphere or a torus of the sizes of the pad. Each returns null when the
    // session applied it, or the rejection for the pad.
    public string? CreateCone(double radius, double height)
        => Rejection(session.Apply(new CreateConeCommand { Radius = radius, Height = height }).GetAwaiter().GetResult());

    public string? CreateSphere(double radius)
        => Rejection(session.Apply(new CreateSphereCommand { Radius = radius }).GetAwaiter().GetResult());

    public string? CreateTorus(double radius, double tubeRadius)
        => Rejection(session.Apply(new CreateTorusCommand { Radius = radius, TubeRadius = tubeRadius }).GetAwaiter().GetResult());

    // ADR-0032 item 4 (Q3 A): a move by the offsets of the pad. Translate consumes the body and
    // makes a new one, and the new body becomes the selection.
    public string? Move(Guid body, double dx, double dy, double dz)
    {
        var result = session.Apply(new TranslateCommand { BodyId = body, Dx = dx, Dy = dy, Dz = dz }).GetAwaiter().GetResult();
        if (result.Status == CommandStatus.Applied && result.Outputs.TryGet<Guid>("bodyId", out var moved))
            Follow(body, moved);
        return Rejection(result);
    }

    // V109 (TASK-0243): the result of GetFaces of the selected body, with its body and the geometry key of the body.
    // ADR-0077 (TASK-0245): the result holds the edges too, so the edges of the selected face need no other query.
    private (Guid Body, Guid Key, FacesInfo Info)? _kept;

    // The faces and the edges of the body, from the kept result when the key is the same.
    private FacesInfo? Info(Guid body, Guid? key)
    {
        if (key is not null && _kept is { } kept && kept.Body == body && kept.Key == key)
            return kept.Info;
        var info = session.Query<FacesInfo>(new GetFacesQuery { BodyId = body }).GetAwaiter().GetResult().Result;
        _kept = info is null || key is null ? null : (body, key.Value, info);
        return info;
    }

    // ADR-0082 item 1 (TASK-0254): each edge of the selected body, from the kept result when the key is the same.
    public IReadOnlyList<EdgeEntry> Edges(Guid? key = null)
        => Selection is { } body && Info(body, key) is { } info ? info.Edges : [];

    // ADR-0082 item 1: the distance of the extrude, kept with the body and its geometry key, so that a frame does not wait
    // for the serial section (V109); a key of null asks each time.
    private (Guid Body, Guid Key, double? Distance)? _keptDistance;

    public double? DistanceOf(Guid body, Guid? key)
    {
        if (key is not null && _keptDistance is { } kept && kept.Body == body && kept.Key == key)
            return kept.Distance;
        var distance = Distance(body);
        _keptDistance = key is null ? null : (body, key.Value, distance);
        return distance;
    }

    // ADR-0077 item 3: the edges of the selected face, each with the face as one of its two names.
    public IReadOnlyList<EdgeEntry> EdgesOfFace(Guid? key = null)
        => Selection is { } body && Face is { } face && Info(body, key) is { } info
            ? [.. info.Edges.Where(edge => edge.First == face || edge.Second == face)]
            : [];

    // ADR-0077 item 4: the entry of the selected edge, or null. A pair that the body no longer has clears the edge.
    public EdgeEntry? SelectedEdge(Guid? key = null)
    {
        if (Selection is not { } body || Edge is not { } edge || Info(body, key) is not { } info)
            return null;
        var entry = info.Edges.FirstOrDefault(candidate => (candidate.First, candidate.Second) == edge);
        if (entry is null)
            Edge = null;
        return entry;
    }

    // ADR-0075 item 3 (phase R12a, TASK-0236): the entry of GetFaces of the selected face, or null. A face whose name the
    // body no longer has, for example after a cut, or a body whose faces are not known, is no longer selected.
    // Finding V109 of the codebase review of 2026-11-06 (d) (TASK-0243): the query waits for the serial section, so a long
    // command of an agent stopped each frame while a face was selected. The entry is kept with the body, the face and the
    // geometry key of the body, and the query runs again only when one of them changes; a key of null asks each time.
    public FaceEntry? SelectedFace(Guid? key = null)
    {
        if (Selection is not { } body || Face is not { } face)
            return null;
        var entry = Info(body, key)?.Faces.FirstOrDefault(candidate => candidate.Reference == face);
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
            Follow(body, cut);
        return Rejection(result);
    }

    // ADR-0101 item 8 (phase R17b, TASK-0315): the new body of a move or a cut becomes the selection, and the focus goes to it
    // when the source was the body of the focus.
    private void Follow(Guid source, Guid made)
    {
        var focused = Focus == source;
        Selection = made;
        if (focused)
            Focus = made;
    }

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

    // ADR-0049 items 7 and 8: the distance in effect of the extrude whose handle is the body, from
    // GetBodyInfo, or null for another body. A new distance from the pad: the body keeps its
    // handle, so the selection stays. Finding V111 (TASK-0243): this comment stood above FaceAt.
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
    //
    // Finding V113 of the codebase review of 2026-11-06 (e) (TASK-0252): a rebuild that fails keeps the geometry key and
    // sets a fault, and GetFaces refuses a body with a fault. The kept entry of such a body is gone, so the next entry
    // comes from the query, which clears the face and the edge; until TASK-0252 the kept entry gave them until the body
    // changed again.
    public void Forget(IReadOnlyList<LiveBody> live)
    {
        if (Selection is { } selected && !live.Any(body => body.Id == selected && body.Visible))
            Selection = null;
        if (_kept is { } kept && live.Any(body => body.Id == kept.Body && body.Fault is not null))
            _kept = null;
    }

    private static void Report(CommandResult result)
    {
        if (result.Status != CommandStatus.Applied)
            Console.WriteLine($"[WARN] The command {result.CommandName} was not applied: {result.Error?.Code} {result.Error?.Message}");
    }
}
