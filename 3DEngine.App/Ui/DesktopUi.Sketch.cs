using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ImGuiNET;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Core.Sketching;

namespace ThreeDEngine.Desktop.Ui;

// Phase R15 (TASK-0234): the window of the sketch (ADR-0034 item 8), a part of DesktopUi. Until TASK-0234 it was in
// DesktopUi.cs; no line changed in the split.
internal sealed unsafe partial class DesktopUi
{
    // ADR-0034 item 8: the window of the sketch. With no active sketch it lists each live sketch
    // with Edit. With an active sketch it gives the tools, the buttons of the selection and a row
    // for each point, line and constraint; a row selects as a tap of the tool Select does.
    private void SketchWindow(SketchPanel panel, Vector2 displaySize)
    {
        ImGui.SetNextWindowPos(new Vector2(Math.Max(16f, displaySize.X - SketchWidth - 16f), 16f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(SketchWidth, Math.Clamp(displaySize.Y - 32f, 200f, 700f)), ImGuiCond.FirstUseEver);
        if (ImGui.Begin(SketchName))
        {
            if (panel.Active is { } sketch)
                ActiveSketch(panel, sketch);
            else
                LiveSketches(panel.Live);
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();
    }

    private void LiveSketches(IReadOnlyCollection<SketchInfo> live)
    {
        ImGui.TextUnformatted(live.Count == 1 ? "1 live sketch" : $"{live.Count} live sketches");
        foreach (var sketch in live)
        {
            var label = $"{EditLabel} {SketchTitle(sketch)}";
            if (ImGui.Button($"{label}##{sketch.SketchId}"))
                _actions.Add(new EditSketchAction(sketch.SketchId));
            Item(SketchItem(label));
        }
    }

    // The name of a sketch, or its short identifier, and its datum.
    public static string SketchTitle(SketchInfo sketch)
        => $"{sketch.Name ?? sketch.SketchId.ToString()[..8]} on {Datums.Find(sketch.DatumId)?.Name ?? "?"}";

    // ADR-0035 item 8: if the solver satisfies each constraint, and the number of free degrees.
    public static string SketchStatus(SketchInfo sketch)
        => sketch.Solved ? $"Solved, {sketch.Freedom} free" : "Not solved";

    private void ActiveSketch(SketchPanel panel, SketchInfo sketch)
    {
        ImGui.TextUnformatted(SketchTitle(sketch));
        ImGui.TextUnformatted(SketchStatus(sketch));
        ToolButton(LineToolLabel, SketchToolKind.Line, panel.Tool);
        ImGui.SameLine();
        ToolButton(SelectToolLabel, SketchToolKind.Select, panel.Tool);
        ImGui.SameLine();
        ToolButton(CircleToolLabel, SketchToolKind.Circle, panel.Tool);
        ImGui.SameLine();
        ToolButton(ArcToolLabel, SketchToolKind.Arc, panel.Tool);

        // The row of Next point stays the second row, so the pad below it stays inside a display of
        // 1024 x 768 (finding V42); Finish and Extrude follow it (TASK-0122).
        if (panel.Tool == SketchToolKind.Line)
        {
            if (ImGui.Button(NextPointLabel))
                OpenNextPointPad(panel.ChainOpen);
            Item(SketchItem(NextPointLabel));
            if (panel.ChainOpen)
            {
                ImGui.SameLine();
                SketchButton(EndLabel, new EndChainAction());
                ImGui.SameLine();
                SketchButton(CloseLabel, new CloseChainAction());
            }
        }

        SketchButton(FinishLabel, new FinishSketchAction());
        ImGui.SameLine();
        // ADR-0036 item 9 (Q4 A): the pad of one signed distance along the normal of the datum.
        if (ImGui.Button(ExtrudeLabel))
            OpenPad(new NumericPad(ExtrudeLabel, _unit, new PadField("distance", "10")), values => new ExtrudeAction(values[0]), bySketch: true, scope: PadScope.Active);
        Item(SketchItem(ExtrudeLabel));

        SelectionButtons(sketch, panel.Selection);
        if (panel.Message is { } message)
            ImGui.TextWrapped(message);

        ImGui.Separator();
        foreach (var (id, text) in SketchText.Rows(sketch))
        {
            if (ImGui.Selectable($"{text}##{id}", panel.Selection.Contains(id), ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                _actions.Add(new ToggleSketchItemAction(id));
            Item(SketchItem(text.Split(' ')[0]));
        }
    }

    // ADR-0087 item 2 (TASK-0276): each kind of constraint that fits the selection, with what its button does: the action of
    // the constraint, or the pad of its value. The window of the sketch and the ring of a sketch both use it.
    private List<(string Label, Action Press)> FittingConstraints(SketchInfo sketch, IReadOnlyList<Guid> selection)
    {
        var list = new List<(string Label, Action Press)>();
        if (selection.Count == 0)
            return list;

        var points = sketch.Points.ToDictionary(point => point.Id);
        var lines = sketch.Lines.Select(line => line.Id).ToHashSet();
        var curves = sketch.Circles.Select(circle => circle.Id).Concat(sketch.Arcs.Select(arc => arc.Id)).ToHashSet();
        int Order(Guid item) => points.ContainsKey(item) ? 0 : lines.Contains(item) ? 1 : curves.Contains(item) ? 2 : 3;
        Guid[] items = [.. selection.OrderBy(Order)];
        var shape = string.Concat(items.Select(item => Order(item) switch { 0 => "P", 1 => "L", 2 => "A", _ => "C" }));
        var origin = selection.Contains(sketch.SketchId);

        foreach (var (label, kind) in ConstraintButtons)
        {
            var fits = (kind, shape) switch
            {
                ("coincident", "PP") => true,
                ("horizontal" or "vertical", "L") => true,
                ("distance", "PP" or "PL" or "L") => true,
                ("fixed", "P") => !origin,
                // Finding V59 of the codebase review of 2026-10-31 (TASK-0133): the engine refuses a point
                // that defines the curve and a line with an end at the centre (SketchConstraints.Refusal).
                ("pointOn", "PA") => label == OnCurveLabel && !CurvePoints(sketch, items[1]).Contains(items[0]),
                ("tangent", "LA") => label != InsideTangentLabel && !IsEnd(sketch, CurvePoints(sketch, items[1])[0], items[0]),
                ("tangent", "AA") => true,
                ("equal", "AA" or "LL") => true,
                ("radius", "A") => true,
                // The kinds of K2c on lines: a point of pointOn and of midpoint is not an end of its
                // line, as the engine requires (SketchConstraints.Refusal). Symmetric takes two points
                // that are not ends of the line, since the selection holds three items (K2g, TASK-0127).
                ("pointOn", "PL") => label == OnLineLabel && !IsEnd(sketch, items[0], items[1]),
                ("midpoint", "PL") => !IsEnd(sketch, items[0], items[1]),
                ("parallel" or "perpendicular" or "angle", "LL") => true,
                ("symmetric", "PPL") => !IsEnd(sketch, items[0], items[2]) && !IsEnd(sketch, items[1], items[2]),
                _ => false,
            };
            if (!fits)
                continue;

            list.Add((label, () =>
            {
                switch (kind)
                {
                    case "distance":
                        var length = SketchText.Distance(sketch, items) ?? 0;
                        OpenPad(new NumericPad(label, _unit, new PadField("length", SketchText.Number(length))), values => new AddConstraintAction(kind, items, values), bySketch: true, scope: PadScope.Sketch);
                        break;
                    case "fixed":
                        var at = points[items[0]];
                        OpenPad(new NumericPad(label, _unit, new("x", SketchText.Number(at.X)), new("y", SketchText.Number(at.Y))), values => new AddConstraintAction(kind, items, values), bySketch: true, scope: PadScope.Sketch);
                        break;
                    case "radius":
                        var radius = SketchText.Radius(sketch, items[0]) ?? 0;
                        OpenPad(new NumericPad(label, _unit, new PadField("radius", SketchText.Number(radius))), values => new AddConstraintAction(kind, items, values), bySketch: true, scope: PadScope.Sketch);
                        break;
                    case "angle":
                        var degrees = SketchText.Angle(sketch, items[0], items[1]) ?? 0;
                        OpenPad(new NumericPad(label, AngleUnit, new PadField("angle", SketchText.Number(degrees))), values => new AddConstraintAction(kind, items, CosineAndSine(values[0])), bySketch: true, scope: PadScope.Sketch);
                        break;
                    case "tangent" when shape == "AA":
                        _actions.Add(new AddConstraintAction(kind, items, [label == InsideTangentLabel ? -1 : 1]));
                        break;
                    default:
                        _actions.Add(new AddConstraintAction(kind, items, []));
                        break;
                }
            }));
        }

        return list;
    }

    // The active tool has the colour of a pressed button.
    private void ToolButton(string label, SketchToolKind kind, SketchToolKind active)
    {
        var pushed = kind == active;
        if (pushed)
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
        SketchButton(label, new SketchToolAction(kind));
        if (pushed)
            ImGui.PopStyleColor();
    }

    private void SketchButton(string label, PanelAction action)
    {
        if (ImGui.Button(label))
            _actions.Add(action);
        Item(SketchItem(label));
    }

    // ADR-0034 item 8: the buttons of the kinds that fit the selection, Move point, Value and
    // Delete. A kind with a value opens the pad with the current value. The items of a constraint
    // go in the order of its kind: the points first.
    private void SelectionButtons(SketchInfo sketch, IReadOnlyList<Guid> selection)
    {
        if (selection.Count == 0)
            return;

        var points = sketch.Points.ToDictionary(point => point.Id);
        var lines = sketch.Lines.Select(line => line.Id).ToHashSet();
        var curves = sketch.Circles.Select(circle => circle.Id).Concat(sketch.Arcs.Select(arc => arc.Id)).ToHashSet();
        var constraint = selection.Count == 1 ? sketch.Constraints.FirstOrDefault(item => item.Id == selection[0]) : null;
        // The points first, then the lines, then the curves (A), as the kinds take their items.
        int Order(Guid item) => points.ContainsKey(item) ? 0 : lines.Contains(item) ? 1 : curves.Contains(item) ? 2 : 3;
        Guid[] items = [.. selection.OrderBy(Order)];
        var shape = string.Concat(items.Select(item => Order(item) switch { 0 => "P", 1 => "L", 2 => "A", _ => "C" }));

        // Finding V43 of the codebase review of 2026-10-29: the origin O of the frame has the
        // identifier of its sketch, and the engine refuses to move it or to delete it (ADR-0033
        // item 2). A selection with O gives no Fixed, Move point or Delete.
        var origin = selection.Contains(sketch.SketchId);

        // The right edge of the content, left of the scroll bar.
        var right = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X;
        var first = true;
        void Flow(string label, Action press)
        {
            // A button goes on the line of the last one when it fits in the window.
            var width = ImGui.CalcTextSize(label).X + 2 * ImGui.GetStyle().FramePadding.X;
            if (!first && ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X + width <= right)
                ImGui.SameLine();
            first = false;
            if (ImGui.Button(label))
                press();
            Item(SketchItem(label));
        }

        // ADR-0087 item 2 (TASK-0276): the window and the ring give the same constraints, from one rule.
        foreach (var (label, press) in FittingConstraints(sketch, selection))
            Flow(label, press);

        if (shape == "P" && !origin)
        {
            var point = points[items[0]];
            Flow(MovePointLabel, () => OpenPad(
                new NumericPad(MovePointLabel, _unit, new("x", SketchText.Number(point.X)), new("y", SketchText.Number(point.Y))),
                values => new MoveSketchPointAction(point.Id, values[0], values[1]),
                bySketch: true,
                scope: PadScope.Sketch));
        }

        // Finding V56 of the codebase review of 2026-10-31 (TASK-0133): the Value pad of an angle showed
        // its cosine and its sine in millimetres. It takes degrees, as the pad of Angle does. A tangent of
        // two curves has the value 1 or −1, which Tangent and Inside tangent give, so it has no Value.
        if (constraint is { Kind: "angle", Values.Count: 2 })
        {
            var degrees = Math.Atan2(constraint.Values[1], constraint.Values[0]) * 180 / Math.PI;
            Flow(ValueLabel, () => OpenPad(
                new NumericPad(ValueLabel, AngleUnit, new PadField("angle", SketchText.Number(degrees))),
                values => new SetConstraintValueAction(constraint.Id, CosineAndSine(values[0])),
                bySketch: true,
                scope: PadScope.Sketch));
        }
        else if (constraint is { Values.Count: > 0 } && constraint.Kind != "tangent")
        {
            string[] names = constraint.Values.Count == 1 ? ["length"] : ["x", "y"];
            Flow(ValueLabel, () => OpenPad(
                new NumericPad(ValueLabel, _unit, [.. constraint.Values.Select((value, index) => new PadField(names[index], SketchText.Number(value)))]),
                values => new SetConstraintValueAction(constraint.Id, values),
                bySketch: true,
                scope: PadScope.Sketch));
        }

        if (!origin)
            Flow(DeleteLabel, () => _actions.Add(new DeleteSketchItemsAction([.. selection])));
    }

    // The centre of a curve first, then the start and the end of an arc.
    private static Guid[] CurvePoints(SketchInfo sketch, Guid curve)
        => sketch.Circles.FirstOrDefault(circle => circle.Id == curve) is { } circle
            ? [circle.Center]
            : sketch.Arcs.FirstOrDefault(arc => arc.Id == curve) is { } arc ? [arc.Center, arc.Start, arc.End] : [Guid.Empty];

    private static bool IsEnd(SketchInfo sketch, Guid point, Guid line)
        => sketch.Lines.FirstOrDefault(item => item.Id == line) is { } found && (found.Start == point || found.End == point);

    // The cosine and the sine of an angle in degrees. A multiple of 90 degrees gives exact numbers, so
    // an angle of 90 records (0, 1) and not (6.1e-17, 1).
    internal static double[] CosineAndSine(double degrees)
    {
        var turn = degrees % 360;
        if (turn < 0)
            turn += 360;
        return turn switch
        {
            0 => [1, 0],
            90 => [0, 1],
            180 => [-1, 0],
            270 => [0, -1],
            // Finding V60 (TASK-0133): the turn, not the degrees, so 1e20 gives the angle of its turn; a
            // value that is not finite gives NaN, which the engine refuses.
            _ => [Math.Cos(turn * Math.PI / 180), Math.Sin(turn * Math.PI / 180)],
        };
    }
}
