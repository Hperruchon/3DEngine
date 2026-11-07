using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Numeric;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0082 (the redesign, slice c, TASK-0254): the values of the selected extrude on its edges. A chip holds the value and
// the unit; a press and a release on one chip open the pad of its value, and the chip is pending while that pad is open.
internal sealed unsafe partial class DesktopUi
{
    // The smallest side of a chip: the 44 pixels of a target of ADR-0027 item 6.
    public const float ValueSize = 44f;

    // The label of the pad of a value of a constraint.
    public const string EdgeValueLabel = "Value";

    // The chips of this frame, and the key of the chip whose pad is open.
    private List<(EdgeValues.Value Value, Vector4 Rectangle)> _chips = [];
    private string? _pendingValue;

    internal string? PendingValue => _pendingValue;

    // True when the point is on a chip of this frame.
    private bool OverValue(double x, double y)
        => _chips.Any(chip => x >= chip.Rectangle.X && x < chip.Rectangle.Z && y >= chip.Rectangle.Y && y < chip.Rectangle.W);

    // Item 1: no chip while a sketch is active, or while the pad of another tool is open.
    private void Values(Vector2 displaySize, IReadOnlyList<EdgeValues.Value>? values)
    {
        _chips = [];
        if (_pad is null)
            _pendingValue = null;
        if (values is not { Count: > 0 } || _sketchActive || (_pad is not null && _pendingValue is null))
            return;

        // Item 2: the chip is centred on its place, and the placement of ADR-0056 moves the chips apart.
        var style = ImGui.GetStyle();
        Vector2 Size(string text)
        {
            var extent = ImGui.CalcTextSize(text) + (style.FramePadding * 2f);
            return new Vector2(Math.Max(extent.X, ValueSize), Math.Max(extent.Y, ValueSize));
        }

        var placed = DimensionOverlay.Place(
            [.. values.Select(value => new DimensionOverlay.Label(value.At - (Size(value.Text) / 2f), value.Text, DimensionOverlay.Mark.Selected))],
            Size, displaySize);
        for (var i = 0; i < values.Count; i++)
        {
            var size = Size(values[i].Text);
            _chips.Add((values[i], OutOfRows(new Vector4(placed[i].At.X, placed[i].At.Y, placed[i].At.X + size.X, placed[i].At.Y + size.Y))));
        }

        // Finding V128 of the codebase review of 2026-11-07 (b) (TASK-0271): the move of ADR-0084 item 4 runs after the
        // placement, so two chips could go to one place, and a tap on the visible chip opened the pad of the other. A chip
        // that covers an earlier chip goes 4 pixels under it.
        for (var i = 1; i < _chips.Count; i++)
        {
            var (value, rectangle) = _chips[i];
            for (var j = 0; j < i; j++)
            {
                var other = _chips[j].Rectangle;
                if (rectangle.X < other.Z && other.X < rectangle.Z && rectangle.Y < other.W && other.Y < rectangle.W)
                {
                    var down = other.W + 4f - rectangle.Y;
                    rectangle = rectangle with { Y = rectangle.Y + down, W = rectangle.W + down };
                    j = -1;
                }
            }

            _chips[i] = (value, rectangle);
        }

        // Item 3: a press and a release on one chip, with no window of the panels under the pointer, open its pad.
        var io = ImGui.GetIO();
        // ADR-0084 item 3 (TASK-0267): a row is drawn over a chip, so a release on a row is the tap of the row only.
        if (_pad is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease()
            && RowAt(io.MousePos) is null && ChipAt(io.MousePos) is { } released && ChipAt(io.MouseClickedPos[0]) is { } pressed && pressed.Key == released.Key)
            Use(released);

        var list = ImGui.GetBackgroundDrawList();
        var colours = style.Colors;
        foreach (var (value, rectangle) in _chips)
        {
            var (min, max) = (new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.Z, rectangle.W));
            var pending = value.Key == _pendingValue;
            var hovered = !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) && ChipAt(io.MousePos)?.Key == value.Key;
            list.AddRectFilled(min, max, ImGui.GetColorU32(colours[(int)(hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button)]), 6f);
            if (pending)
                Dashed(list, min, max, DimensionColour);
            else
                list.AddRect(min, max, ImGui.GetColorU32(colours[(int)ImGuiCol.Border]), 6f, ImDrawFlags.None, 1.5f);
            var text = ImGui.CalcTextSize(value.Text);
            list.AddText(((min + max) / 2f) - (text / 2f), pending ? DimensionColour : ImGui.GetColorU32(colours[(int)ImGuiCol.Text]), value.Text);
            _items[value.Key] = rectangle;
        }
    }

    private EdgeValues.Value? ChipAt(Vector2 point)
        => _chips.FirstOrDefault(chip => point.X >= chip.Rectangle.X && point.X < chip.Rectangle.Z && point.Y >= chip.Rectangle.Y && point.Y < chip.Rectangle.W).Value;

    // Item 3: the distance asks the host for its pad, as the segment Distance of the ring does; a constraint opens the
    // pad of its value, whose apply changes the constraint of the sketch of the extrude.
    private void Use(EdgeValues.Value value)
    {
        if (value.Constraint is { } constraint && value.Sketch is { } sketch)
        {
            OpenPad(new NumericPad(EdgeValueLabel, _unit, new PadField("value", SketchText.Number(value.Number))),
                numbers => new SetBodyValueAction(sketch, constraint, numbers[0]), scope: PadScope.Body);
            _pendingValue = value.Key;
        }
        else
            _actions.Add(new EditDistanceAction(value.Body));
    }

    // Item 4: the border of a pending chip, as dashes of 6 pixels with gaps of 4 pixels.
    private static void Dashed(ImDrawListPtr list, Vector2 min, Vector2 max, uint colour)
    {
        Vector2[] corners = [min, new(max.X, min.Y), max, new(min.X, max.Y)];
        for (var side = 0; side < 4; side++)
        {
            var (a, b) = (corners[side], corners[(side + 1) % 4]);
            var length = Vector2.Distance(a, b);
            for (var at = 0f; at < length; at += 10f)
                list.AddLine(a + ((b - a) * (at / length)), a + ((b - a) * (Math.Min(at + 6f, length) / length)), colour, 2f);
        }
    }
}
