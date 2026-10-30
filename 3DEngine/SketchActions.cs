using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Core.Sketching;

namespace ThreeDEngine.Desktop;

// The sketch tools of the desktop (ADR-0034, TASK-0101): the active sketch, the tool, and the
// commands of the session that the tool and the window of the sketch ask for. The active sketch,
// the tool and its selection are states of the client (ADR-0007); each change of a sketch is a
// command of the session, the same command that an agent sends (ADR-0019 item 2), so the log
// records each one. The desktop calls this on its render loop, outside each commit.
internal sealed class SketchActions(DocumentSession session) : ISketchEditor
{
    // The active sketch, or null when the intent layer is in the mode Scene.
    public Guid? Active { get; private set; }

    public SketchTool Tool { get; } = new();

    // The rejection of the last command, for the window of the sketch, or null.
    public string? Message { get; private set; }

    // ADR-0034 item 6: a new sketch on a datum becomes the active sketch. It gives the sketch, or
    // null when the session refused the command.
    public Guid? Create(Guid datum)
    {
        var result = Run(new CreateSketchCommand { DatumId = datum });
        if (result.Status != CommandStatus.Applied || !result.Outputs.TryGet<Guid>("sketchId", out var sketch))
            return null;
        Edit(sketch);
        return sketch;
    }

    public void Edit(Guid sketch)
    {
        Active = sketch;
        Reset();
    }

    public void Finish()
    {
        Active = null;
        Reset();
    }

    private void Reset()
    {
        Tool.Kind = SketchToolKind.Line;
        Tool.ClearSelection();
        Message = null;
    }

    // The record of the active sketch with the residual of each constraint, read now, so that two
    // taps of one frame each see the point of the other.
    public SketchInfo? Current()
        => Active is { } sketch ? session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch }).GetAwaiter().GetResult().Result : null;

    // ADR-0034 items 2 to 4: a sketch point intent at a pixel of a window of this size. The ray of
    // the pixel meets the plane of the datum, and the position is rounded to the step of the zoom.
    public void Tap(Camera camera, double x, double y, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(camera);
        if (Current() is not { } sketch || Datums.Find(sketch.DatumId) is not { } datum || !(width > 0 && height > 0))
            return;

        Tool.Tap(x, y, Position(datum, camera, x, y, width, height), SketchOverlay.Picture(sketch, camera, width, height), this);
    }

    // The rounded position of a pixel on the plane of the datum, or null. The step comes from the
    // millimetres between the positions of this pixel and of the next pixel to the right.
    private static (double X, double Y)? Position(Datum datum, Camera camera, double x, double y, double width, double height)
    {
        var (origin, u, v) = ((0.0, 0.0, 0.0), (datum.U.X, datum.U.Y, datum.U.Z), (datum.V.X, datum.V.Y, datum.V.Z));
        if (Picker.OnPlane(Picker.RayThrough(camera, x, y, width, height), origin, u, v) is not { } at)
            return null;

        var step = Picker.OnPlane(Picker.RayThrough(camera, x + 1, y, width, height), origin, u, v) is { } next
            ? SketchGrid.Step(1 / Math.Sqrt((next.X - at.X) * (next.X - at.X) + (next.Y - at.Y) * (next.Y - at.Y)))
            : SketchGrid.Series[0];
        return (step.Round(at.X), step.Round(at.Y));
    }

    // ADR-0034 item 7: the next point from the pad, and Close.
    public string? Next(double a, double b)
    {
        Message = null;
        if (Current() is { } sketch)
            Tool.Next(a, b, SketchOverlay.Picture(sketch), this);
        return Message;
    }

    public void Close() => Tool.Close(this);

    public Guid? AddPoint(double x, double y)
        => Output(Active is { } sketch ? Run(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }) : null, "pointId");

    public Guid? AddLine(Guid start, Guid end)
        => Output(Active is { } sketch ? Run(new AddSketchLineCommand { SketchId = sketch, StartPointId = start, EndPointId = end }) : null, "lineId");

    // Phase K2e (TASK-0122): the curves of the tools Circle and Arc.
    public Guid? AddCircle(Guid centre, double radius)
        => Output(Active is { } sketch ? Run(new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = radius }) : null, "circleId");

    public Guid? AddArc(Guid centre, Guid start, Guid end)
        => Output(Active is { } sketch ? Run(new AddSketchArcCommand { SketchId = sketch, CenterPointId = centre, StartPointId = start, EndPointId = end }) : null, "arcId");

    // ADR-0034 item 8: a constraint of a kind on the items, in their order, with its values.
    public string? AddConstraint(string kind, IReadOnlyList<Guid> items, IReadOnlyList<double> values)
    {
        if (Active is not { } sketch || items.Count is < 1 or > 3)
            return null;
        return Rejection(Run(new AddSketchConstraintCommand
        {
            SketchId = sketch,
            Kind = kind,
            First = items[0],
            Second = items.Count > 1 ? items[1] : null,
            Third = items.Count > 2 ? items[2] : null,
            Value = values.Count > 0 ? values[0] : null,
            SecondValue = values.Count > 1 ? values[1] : null,
        }));
    }

    public string? SetValue(Guid constraint, IReadOnlyList<double> values)
        => Active is { } sketch && values.Count > 0
            ? Rejection(Run(new SetSketchConstraintValueCommand { SketchId = sketch, ConstraintId = constraint, Value = values[0], SecondValue = values.Count > 1 ? values[1] : null }))
            : null;

    public string? MovePoint(Guid point, double x, double y)
        => Active is { } sketch ? Rejection(Run(new MoveSketchPointCommand { SketchId = sketch, PointId = point, X = x, Y = y })) : null;

    // ADR-0036 item 9: an extrude of the active sketch. A success ends the mode Sketch and gives
    // the new body; a rejection keeps the mode and gives the text for the pad.
    public (string? Rejection, Guid? Body) Extrude(double distance)
    {
        if (Active is not { } sketch)
            return (null, null);
        var result = Run(new ExtrudeCommand { SketchId = sketch, Distance = distance });
        if (Output(result, "bodyId") is not { } body)
            return (Rejection(result), null);
        Finish();
        return (null, body);
    }

    // One command for each item. An item that an earlier delete removed with its point is gone
    // already, so its refusal is not shown.
    public void Delete(IReadOnlyList<Guid> items)
    {
        if (Active is not { } sketch)
            return;
        string? first = null;
        foreach (var item in items)
        {
            var result = Run(new DeleteSketchItemCommand { SketchId = sketch, ItemId = item });
            if (result.Status != CommandStatus.Applied && result.Error?.Code != DiagnosticCodes.SketchItemNotFound)
                first ??= Text(result);
        }

        Tool.ClearSelection();
        Message = first;
    }

    // ADR-0034 items 5 and 6: after the events of a frame, the tool keeps only the items of the
    // record, and the mode ends when the active sketch left the session.
    public void Follow(IReadOnlyCollection<SketchInfo> live)
    {
        ArgumentNullException.ThrowIfNull(live);
        if (Active is not { } active)
            return;
        if (live.FirstOrDefault(sketch => sketch.SketchId == active) is { } sketch)
            Tool.Forget(SketchOverlay.Picture(sketch));
        else
            Finish();
    }

    private CommandResult Run(Command command)
    {
        var result = session.Apply(command).GetAwaiter().GetResult();
        Message = result.Status == CommandStatus.Applied ? null : Text(result);
        if (Message is not null)
            Console.WriteLine($"[WARN] The command {result.CommandName} was not applied: {Message}");
        return result;
    }

    private static Guid? Output(CommandResult? result, string name)
        => result is { Status: CommandStatus.Applied } && result.Outputs.TryGet<Guid>(name, out var id) ? id : null;

    private static string? Rejection(CommandResult result) => result.Status == CommandStatus.Applied ? null : Text(result);

    private static string Text(CommandResult result) => $"{result.Error?.Code}: {result.Error?.Message}";
}
