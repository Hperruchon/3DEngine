using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Numeric;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0082 (the redesign, slice c, TASK-0254): the values of the selected extrude on its edges. A chip holds the value and
// the unit; a press and a release on one chip open the pad of its value, and the chip is pending while that pad is open.
// ADR-0105 (TASK-0327): while a sketch is active, the chips are the values of that sketch on its lines and its curves.
// ADR-0110 item 2 (TASK-0334): a chip whose value the shown proposal changes shows the value now and the proposed value.
// ADR-0115 (phase R18c, TASK-0340): each value is drawn as in the render: a dimension line parallel to its edge outside the
// outline of the body, an extension line from each end of the edge, and an outlined pill on the line.
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

    // ADR-0110 item 2 (TASK-0334): the keys of the chips of this frame whose value the shown proposal changes.
    private HashSet<string> _proposedChips = new(StringComparer.Ordinal);

    // The key, the text and the pending style of each chip of this frame, for a test.
    internal IReadOnlyList<(string Key, string Text, bool Pending)> Chips => [.. _chips.Select(chip => (chip.Value.Key, chip.Value.Text, Pending(chip.Value.Key)))];

    // ADR-0115 item 2: the padding of the text in a pill, and the length of an extension line past the dimension line.
    internal static readonly Vector2 PillPadding = new(10f, 5f);
    internal const float ExtensionPast = 6f;
    internal const float ExtensionGap = 4f;

    // ADR-0115 item 2: the dimension line and the drawn pill of each value of this frame with an edge, for a test: the two
    // ends of the line (one point for the leader of a radius, with the end at the pill) and the rectangle of the pill.
    private List<(string Key, Vector2 From, Vector2 To, Vector4 Pill)> _dimensionLines = [];

    internal IReadOnlyList<(string Key, Vector2 From, Vector2 To, Vector4 Pill)> DimensionLines => _dimensionLines;

    // Item 4: a chip is pending while its pad is open. ADR-0110 item 2 (TASK-0334): a chip is pending too while the shown
    // proposal changes its value.
    private bool Pending(string key) => key == _pendingValue || _proposedChips.Contains(key);

    // True when the point is on a chip of this frame.
    private bool OverValue(double x, double y)
        => _chips.Any(chip => x >= chip.Rectangle.X && x < chip.Rectangle.Z && y >= chip.Rectangle.Y && y < chip.Rectangle.W);

    // Item 1: no chip of the extrude while a sketch is active, or while the pad of another tool is open. ADR-0105 item 1
    // (TASK-0327): while a sketch is active, the chips are the values of that sketch.
    private void Values(Vector2 displaySize, IReadOnlyList<EdgeValues.Value>? edgeValues, IReadOnlyList<EdgeValues.Value>? sketchValues)
    {
        _chips = [];
        _dimensionLines = [];
        _proposedChips = new(StringComparer.Ordinal);
        if (_pad is null)
            _pendingValue = null;
        var values = _sketchActive ? sketchValues : edgeValues;
        if (values is not { Count: > 0 } || (_pad is not null && _pendingValue is null))
            return;

        // ADR-0110 item 2 (Q2 A, TASK-0334): while the window shows a proposal, each chip whose key is a key of a proposed
        // value of that proposal shows "label  now → proposed unit". The value of the chip stays the value now, so a tap on it
        // opens the pad that it opened before.
        var proposed = _proposals.Count > 0 && _proposals[0].Values is { } commands ? ProposedValues.Of(commands) : [];
        var shown = new List<EdgeValues.Value>(values.Count);
        foreach (var value in values)
        {
            if (proposed.FirstOrDefault(change => change.Keys.Contains(value.Key)) is { } change)
            {
                _proposedChips.Add(value.Key);
                shown.Add(value with { Text = PendingText(value.Text, change.Number) });
            }
            else
                shown.Add(value);
        }

        values = shown;

        // Item 2: the chip is centred on its place, and the placement of ADR-0056 moves the chips apart.
        var style = ImGui.GetStyle();
        // ADR-0115 item 3: the drawn pill is the text and its padding; the target around it is at least 44 pixels on each side.
        Vector2 PillSize(string text)
        {
            // ADR-0091: a value has the font of a value, JetBrains Mono.
            ImGui.PushFont(_monoFont);
            var extent = new Vector2(ArrowTextWidth(_monoFont, FontSize, text), ImGui.CalcTextSize(text).Y) + (PillPadding * 2f);
            ImGui.PopFont();
            return extent;
        }

        Vector2 Size(string text)
        {
            var pill = PillSize(text);
            return new Vector2(Math.Max(pill.X, ValueSize), Math.Max(pill.Y, ValueSize));
        }

        // ADR-0115 item 2: the dimension line goes past the outline by the gap and the half of the pill along its normal, so
        // the pill is outside the outline and centred on the line.
        values = [.. values.Select(value => value.Line is { } line
            ? value with { At = EdgeValues.Placed(line) + (line.Normal * Half(PillSize(value.Text), line.Normal)) }
            : value)];

        // ADR-0135 item 4 (phase R18o, TASK-0368): while a proposal waits on a portrait display, the chips keep off the arc,
        // the sheet and the rows of the last frame, which do not move for a chip, so the person reads the value that the
        // proposal changes. A landscape display moves the lines of the arc off the chips instead.
        IReadOnlyList<Vector4>? covers = displaySize.X < displaySize.Y && _proposals.Count > 0
            ? [.. _lastProposalCovers, .. _lastSheetBox is { } sheet ? [sheet] : Array.Empty<Vector4>(), .. _rowsArea is { } rows ? [rows] : Array.Empty<Vector4>()]
            : null;
        var placed = DimensionOverlay.Place(
            [.. values.Select(value => new DimensionOverlay.Label(value.At - (Size(value.Text) / 2f), value.Text, DimensionOverlay.Mark.Selected))],
            Size, displaySize, covers);
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
        var lineColour = ImGui.GetColorU32(colours[(int)ImGuiCol.Border]);
        foreach (var (value, rectangle) in _chips)
        {
            var pending = Pending(value.Key);
            var hovered = !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) && ChipAt(io.MousePos)?.Key == value.Key;
            // ADR-0115 item 3: the drawn pill is centred in the target of the chip.
            var middle = new Vector2((rectangle.X + rectangle.Z) / 2f, (rectangle.Y + rectangle.W) / 2f);
            var pillSize = PillSize(value.Text);
            var (min, max) = (middle - (pillSize / 2f), middle + (pillSize / 2f));
            var round = pillSize.Y / 2f;
            var ink = pending ? DimensionColour : lineColour;

            // ADR-0115 item 2: the extension lines from the ends of the edge, 4 pixels off it and 6 pixels past the dimension
            // line, and the dimension line through the middle of the pill; a radius has a leader from its curve to the pill.
            if (value.Line is { } line)
            {
                var shift = Vector2.Dot(middle - ((line.A + line.B) / 2f), line.Normal);
                var (from, to) = (line.A + (line.Normal * shift), line.B + (line.Normal * shift));
                if (line.Leader)
                {
                    list.AddLine(line.A + (line.Normal * ExtensionGap), middle, ink, 1f);
                    (from, to) = (line.A, middle);
                }
                else
                {
                    list.AddLine(line.A + (line.Normal * ExtensionGap), from + (line.Normal * ExtensionPast), ink, 1f);
                    list.AddLine(line.B + (line.Normal * ExtensionGap), to + (line.Normal * ExtensionPast), ink, 1f);
                    list.AddLine(from, to, ink, 1f);
                }

                _dimensionLines.Add((value.Key, from, to, new Vector4(min.X, min.Y, max.X, max.Y)));
            }

            // ADR-0108 item 2 (phase R18a, TASK-0332): a chip lies over the body, so it has the ground of the scene and no
            // blue fill, as the render; a hover adds the light fill of the theme. Until TASK-0332 it had a blue ground.
            // ADR-0115 item 3: the pill is round at its ends, as the render.
            list.AddRectFilled(min, max, Theme.Plate(0.85f), round);
            if (hovered)
                list.AddRectFilled(min, max, ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), round);
            if (pending)
                Dashed(list, min, max, DimensionColour);
            else
                list.AddRect(min, max, lineColour, round, ImDrawFlags.None, 1.5f);
            ImGui.PushFont(_monoFont);
            var text = new Vector2(ArrowTextWidth(_monoFont, FontSize, value.Text), ImGui.CalcTextSize(value.Text).Y);
            ImGui.PopFont();
            ArrowText(list, _monoFont, FontSize, ((min + max) / 2f) - (text / 2f), pending ? DimensionColour : ImGui.GetColorU32(colours[(int)ImGuiCol.Text]), value.Text);
            _items[value.Key] = rectangle;
        }
    }

    // ADR-0115 item 2: the half of the extent of a pill along a unit direction.
    internal static float Half(Vector2 pill, Vector2 direction) => ((Math.Abs(direction.X) * pill.X) + (Math.Abs(direction.Y) * pill.Y)) / 2f;

    private EdgeValues.Value? ChipAt(Vector2 point)
        => _chips.FirstOrDefault(chip => point.X >= chip.Rectangle.X && point.X < chip.Rectangle.Z && point.Y >= chip.Rectangle.Y && point.Y < chip.Rectangle.W).Value;

    // Item 3: the distance asks the host for its pad, as the segment Distance of the ring does; a constraint opens the
    // pad of its value, whose apply changes the constraint of the sketch of the extrude.
    // ADR-0128 item 2 (phase R18h, TASK-0357): a value typed in the command line is the start text of the pad, and the
    // distance opens its pad here with that text; with no typed text the pad starts at the value now.
    private void Use(EdgeValues.Value value, string? typed = null)
    {
        var start = typed ?? SketchText.Number(value.Number);
        // ADR-0105 item 4 (TASK-0327): a chip of the active sketch opens the pad that the row of its constraint opens
        // (ADR-0034 item 8), with the same apply, and the chip is pending while that pad is open.
        if (_sketchActive && value.Constraint is { } sketchConstraint)
        {
            OpenPad(new NumericPad(ValueLabel, _unit, new PadField("length", start)),
                numbers => new SetConstraintValueAction(sketchConstraint, numbers), bySketch: true, scope: PadScope.Sketch);
            _pendingValue = value.Key;
        }
        else if (value.Constraint is { } constraint && value.Sketch is { } sketch)
        {
            OpenPad(new NumericPad(EdgeValueLabel, _unit, new PadField("value", start)),
                numbers => new SetBodyValueAction(sketch, constraint, numbers[0]), scope: PadScope.Body);
            _pendingValue = value.Key;
        }
        else if (typed is not null)
            OpenDistancePad(value.Body, typed);
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
