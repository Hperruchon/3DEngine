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
    // The rectangle of the window of the sketch of the last frame, for the tests (T176, TASK-0296).
    public Vector4? SketchWindowRectangle => _sketchWindow;

    // ADR-0034 item 8: the window of the sketch. With no active sketch it lists each live sketch
    // with Edit. With an active sketch it gives the tools, the buttons of the selection and a row
    // for each point, line and constraint; a row selects as a tap of the tool Select does.
    private void SketchWindow(SketchPanel panel, Vector2 displaySize)
    {
        // ADR-0097 item 1 (the redesign, slice n, TASK-0308): the rows of an active sketch take the place of the panel, under
        // the top bar at the left, with no title bar, no ground and no border, as the panel. The window of the
        // live sketches stays as before.
        if (panel.Active is { } active)
        {
            ActiveSketchRows(panel, active, displaySize);
            return;
        }

        // Finding V152 of the codebase review of 2026-11-09 (TASK-0324): the window of the live sketches is at the right edge of
        // the safe area, 8 pixels under the field of the command line when the field shows, else at the top. The frame sets
        // its place in each frame, as the other windows of the mix, and its height ends above the row of the anchors. Until
        // TASK-0324 its first frame put it at the upper right, over the command line, and the field took no click.
        var top = ItemRectangle(CommandItem) is { } field ? field.W + 8f : 16f + _inset.Y;
        ImGui.SetNextWindowPos(new Vector2(displaySize.X - 16f - _inset.Z, top), ImGuiCond.Always, new Vector2(1f, 0f));
        // Finding T176 of the codebase review of 2026-11-08 (TASK-0296): the window is at most as wide as the display less
        // its margins. Until TASK-0296 it was 480 wide on a display of 390, and Arc was outside the display.
        var width = Math.Min(SketchWidth, Math.Max(displaySize.X - 32f - _inset.X - _inset.Z, 160f));
        var room = Math.Max(displaySize.Y - top - 16f - AnchorRow - _inset.W, 0f);
        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(width, room));
        ImGui.SetNextWindowSize(new Vector2(width, Math.Min(Math.Clamp(displaySize.Y - 32f, 200f, 700f), room)), ImGuiCond.FirstUseEver);
        if (ImGui.Begin(SketchName, ImGuiWindowFlags.NoMove))
            LiveSketches(panel.Live);

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        _sketchWindow = _panels[^1];
        ImGui.End();
    }

    // ADR-0097 item 1: the window of the active sketch in the place of the panel. Its height is its content in the last frame,
    // inside the room above the anchors, as the panel.
    private void ActiveSketchRows(SketchPanel panel, SketchInfo sketch, Vector2 displaySize)
    {
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X, PanelTop), ImGuiCond.Always);
        var width = Math.Min(SketchWidth, Math.Max(displaySize.X - 32f - _inset.X - _inset.Z, 160f));
        var room = Math.Max(displaySize.Y - PanelTop - 16f - AnchorRow - _inset.W, 0f);
        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(width, room));
        ImGui.SetNextWindowSize(new Vector2(width, Math.Min(_sketchHeight ?? room, room)), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        // The scrollbar of a long list has no ground, as the window has none.
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, 0u);
        var open = ImGui.Begin($"{SketchName}###{SketchName}", PanelFlags);
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
        if (open)
        {
            ActiveSketch(panel, sketch);
            _sketchHeight = ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y;
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        _sketchWindow = _panels[^1];
        ImGui.End();
    }

    // The height of the content of the window of the active sketch in the last frame (ADR-0097 item 1).
    private float? _sketchHeight;

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

    // ADR-0120 item 1 (phase R18f, TASK-0347): the state of a sketch as the render gives it after its name: "solved · 2 free"
    // in green, or "not solved" in red.
    internal static string SketchState(SketchInfo sketch) => sketch.Solved ? $"solved · {sketch.Freedom} free" : "not solved";

    // ADR-0120 item 1: the item of the row "Makes", and of the row of a constraint.
    public const string MakesItem = "Sketch:makes";
    public const string MakesLabel = "Makes";

    public static string ConstraintRowItem(Guid constraint) => $"SketchRow:{constraint:N}";

    // ADR-0120 item 1: the text of a row of a constraint at its left and at its right: "C3 distance P1 P2" and "60.000 mm",
    // or "C1 horizontal L1" and "r 0". An angle shows degrees; a constraint with no value shows its residual.
    internal (string Left, string Right) ConstraintRow(SketchInfo sketch, SketchConstraintInfo constraint, IReadOnlyDictionary<Guid, string> names)
    {
        var items = string.Join(" ", constraint.Items.Select(item => names.TryGetValue(item, out var name) ? name : "?"));
        var left = $"{names.GetValueOrDefault(constraint.Id, "C")} {constraint.Kind} {items}";
        var right = constraint switch
        {
            { Kind: "angle", Values.Count: 2 } => $"{EdgeValues.Exact(Math.Atan2(constraint.Values[1], constraint.Values[0]) * 180 / Math.PI)} {AngleUnit}",
            { Kind: "tangent" } or { Values.Count: 0 } => $"r {SketchText.Short(constraint.Residual ?? 0)}",
            { Values.Count: 1 } => $"{EdgeValues.Exact(constraint.Values[0])} {_unit}",
            _ => string.Join(", ", constraint.Values.Select(EdgeValues.Exact)) + $" {_unit}",
        };
        return (left, right);
    }

    private void ActiveSketch(SketchPanel panel, SketchInfo sketch)
    {
        // ADR-0097 item 2: the name and the state of the sketch on one line, as the render. ADR-0120 item 1 (TASK-0347): the
        // name in the font of a title, drawn a second time 1 pixel to the right, and the state in green or in red.
        var list = ImGui.GetWindowDrawList();
        var start = ImGui.GetCursorScreenPos();
        var title = SketchTitle(sketch);
        var titleWidth = TitleWidth(title);
        ImGui.Dummy(new Vector2(titleWidth, TitleSize));
        list.AddText(_titleFont, TitleSize, start, ImGui.GetColorU32(ImGuiCol.Text), title);
        list.AddText(_titleFont, TitleSize, start + new Vector2(1f, 0f), ImGui.GetColorU32(ImGuiCol.Text), title);
        ImGui.SameLine(0f, TitleGap);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + TitleSize - ImGui.GetTextLineHeight() - 2f);
        ImGui.PushStyleColor(ImGuiCol.Text, sketch.Solved ? Theme.Correct : FailedColour);
        ImGui.TextUnformatted(SketchState(sketch));
        ImGui.PopStyleColor();
        // ADR-0088 item 1 (the redesign, slice h, TASK-0277): while the ring of the sketch shows in this frame, it holds the
        // tools, Finish and the kinds of a constraint, so the window does not show them again. The ring comes first in the
        // frame, so _ring is the ring of this frame.
        // V134 (TASK-0280): only when a tap reaches each segment; until TASK-0280 a ring under the window or below the
        // display left the person with no Finish.
        var ringHolds = _ring is not null && _ringReachable;
        // Finding V140 of the codebase review of 2026-11-08 (TASK-0296): the second ring holds the kinds and Back, not the
        // tools and Finish, so while it is open the window shows them. Until TASK-0296 the person tapped Back first.
        var toolsOnRing = ringHolds && !_constrainOpen;
        if (!toolsOnRing)
        {
            ToolButton(LineToolLabel, SketchToolKind.Line, panel.Tool);
            ImGui.SameLine();
            ToolButton(SelectToolLabel, SketchToolKind.Select, panel.Tool);
            ImGui.SameLine();
            ToolButton(CircleToolLabel, SketchToolKind.Circle, panel.Tool);
            ImGui.SameLine();
            ToolButton(ArcToolLabel, SketchToolKind.Arc, panel.Tool);
        }

        // The row of Next point stays near the top: the second row, or the first while the ring holds the tools (ADR-0088),
        // so the pad below it stays inside a display of 1024 x 768 (finding V42); Finish and Extrude follow it (TASK-0122).
        // Finding V138 of the codebase review of 2026-11-07 (c) (TASK-0282): the comment said the second row only.
        // ADR-0120 item 2: while the ring holds the tools, "Next point" and "Extrude" are on it; End and Close stay here.
        if (panel.Tool == SketchToolKind.Line && (!toolsOnRing || panel.ChainOpen))
        {
            if (!toolsOnRing && ImGui.Button(NextPointLabel))
                OpenNextPointPad(panel.ChainOpen);
            if (!toolsOnRing)
                Item(SketchItem(NextPointLabel));
            if (panel.ChainOpen)
            {
                if (!toolsOnRing)
                    ImGui.SameLine();
                SketchButton(EndLabel, new EndChainAction());
                ImGui.SameLine();
                SketchButton(CloseLabel, new CloseChainAction());
            }
        }

        if (!toolsOnRing)
        {
            SketchButton(FinishLabel, new FinishSketchAction());
            ImGui.SameLine();
        }

        // ADR-0036 item 9 (Q4 A): the pad of one signed distance along the normal of the datum. ADR-0120 item 2: on the ring
        // while it holds the tools.
        if (!toolsOnRing)
        {
            if (ImGui.Button(ExtrudeLabel))
                OpenExtrudePad();
            Item(SketchItem(ExtrudeLabel));
        }

        SelectionButtons(sketch, panel.Selection, ringHolds);
        if (panel.Message is { } message)
            ImGui.TextWrapped(message);

        // ADR-0120 item 1 (phase R18f, TASK-0347): the rows are the constraints only, as the render, each with its value or its
        // residual at the right in the mono font, and a line under each; a tap on a row selects the constraint, as before. A
        // point, a line and a curve are selected on the sketch with the tool Select. Until TASK-0347 the rows listed each point,
        // each line and each constraint, and the list scrolled.
        var names = SketchText.Labels(sketch);
        var colours = ImGui.GetStyle().Colors;
        var separator = ImGui.GetColorU32(colours[(int)ImGuiCol.Separator]);
        var pad = (MinimumControlHeight - ImGui.GetTextLineHeight()) / 2f;
        foreach (var constraint in sketch.Constraints)
        {
            var (left, right) = ConstraintRow(sketch, constraint, names);
            var selected = panel.Selection.Contains(constraint.Id);
            if (ImGui.Selectable($"##row{constraint.Id}", selected, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                _actions.Add(new ToggleSketchItemAction(constraint.Id));
            var (min, max) = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            list.AddText(new Vector2(min.X, min.Y + pad), ImGui.GetColorU32(ImGuiCol.Text), left);
            var width = _monoFont.CalcTextSizeA(FontSize, float.MaxValue, 0f, right).X;
            list.AddText(_monoFont, FontSize, new Vector2(max.X - width, min.Y + pad), ImGui.GetColorU32(right.StartsWith("r ", StringComparison.Ordinal) ? ImGuiCol.TextDisabled : ImGuiCol.Text), right);
            list.AddLine(new Vector2(min.X, max.Y), max, separator, 1f);
            Item(ConstraintRowItem(constraint.Id));
            Item(SketchItem(names.GetValueOrDefault(constraint.Id, "C")));
        }

        // ADR-0120 item 2: the row "Makes" with the bodies that the sketch makes, in the accent; a tap ends the sketch and
        // selects the body.
        foreach (var made in panel.Makes ?? [])
        {
            if (ImGui.Selectable($"##makes{made.Id}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
            {
                _actions.Add(new FinishSketchAction());
                _actions.Add(new SelectBodyAction(made.Id));
            }

            var (min, max) = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            list.AddText(new Vector2(min.X, min.Y + pad), ImGui.GetColorU32(ImGuiCol.TextDisabled), MakesLabel);
            var name = Fit(made.Text, max.X - min.X - ImGui.CalcTextSize(MakesLabel).X - TitleGap);
            list.AddText(new Vector2(max.X - ImGui.CalcTextSize(name).X, min.Y + pad), Theme.Accent, name);
            list.AddLine(new Vector2(min.X, max.Y), max, separator, 1f);
            Item(MakesItem);
        }
    }

    // ADR-0120 item 2 (phase R18f, TASK-0347): the pad of an extrude, from the window, the ring or the command line.
    private void OpenExtrudePad() => OpenExtrudePad("10");

    // ADR-0128 item 2 (phase R18h, TASK-0357): the same pad with a start text, which a value typed in the command line gives.
    private void OpenExtrudePad(string start)
        => OpenPad(new NumericPad(ExtrudeLabel, _unit, new PadField("distance", start)), values => new ExtrudeAction(values[0]), bySketch: true, scope: PadScope.Active);

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
    private void SelectionButtons(SketchInfo sketch, IReadOnlyList<Guid> selection, bool ringHolds)
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

        // ADR-0087 item 2 (TASK-0276): the window and the ring give the same constraints, from one rule. ADR-0088 item 1: while
        // the ring shows, the kinds are on the ring only.
        foreach (var (label, press) in ringHolds ? [] : FittingConstraints(sketch, selection))
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
