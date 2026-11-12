using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0104 (the redesign, phase R16q, TASK-0325): the history arc of the render "mix". While a body is selected on a
// landscape display, a quarter arc inside the guide circle of its ring, from the left to the bottom, holds one node for each
// step that made the body (HistorySteps), the newest at the upper end, with its text beside it and the caption "HISTORY"
// under the lower end. A press and a release on one node of a sketch open the edit of that sketch; on one node of a live
// body that is not consumed, they select that body. The nodes and their texts are a part of the panels for a tap.
internal sealed unsafe partial class DesktopUi
{
    // The diameter of a node: the 44 pixels of a target of ADR-0027 item 6. ADR-0119 item 3 (phase R18e, TASK-0346): the
    // target; the drawn node is smaller, on the arc.
    public const float HistoryNode = ValueSize;

    // ADR-0119 item 3: the radius of the drawn node of the selected body, and of each other node.
    internal const float HistoryDotSelected = 10f;
    internal const float HistoryDot = 6f;

    // ADR-0119 item 3: the accent of the theme with an alpha from 0 to 1.
    // ADR-0116 item 2 (phase R20a, TASK-0341): ImGui.GetColorU32 multiplies the alpha by the alpha of the style, so the arc fades
    // with the ring while the view turns, pans or zooms.
    private static uint AccentAlpha(float alpha) => ImGui.GetColorU32((Theme.Accent & 0x00FFFFFFu) | ((uint)(alpha * 255f) << 24));

    // The space between the edge of a node and the inner guide circle of the ring, and between a node and its text.
    private const float HistoryTextGap = 8f;

    public const string HistoryCaptionItem = "History:caption";

    // The item of the node of a step, and of its text.
    public static string HistoryItem(Guid step) => $"History:{step}";

    public static string HistoryTextItem(Guid step) => $"HistoryText:{step}";

    // The nodes of this frame: the step, the centre of its node, and the rectangle of its text.
    private List<(HistoryStep Step, Vector2 Centre, Vector4 Text)> _historyNodes = [];

    // True when the point is on a node or on its text in this frame.
    private bool OverHistory(double x, double y) => HistoryAt(new Vector2((float)x, (float)y)) is not null;

    private HistoryStep? HistoryAt(Vector2 point)
        => _historyNodes.FirstOrDefault(node => Vector2.Distance(point, node.Centre) <= HistoryNode / 2f
            || (point.X >= node.Text.X && point.X < node.Text.Z && point.Y >= node.Text.Y && point.Y < node.Text.W)).Step;

    // Item 3: the arc of the selected body, with the ring of this frame. None on a portrait display, while a sketch is active,
    // while a pad is open, and with no ring.
    private void History(Vector2 displaySize, LiveBody? body, IReadOnlyList<HistoryStep>? steps)
    {
        _historyNodes = [];
        HistoryAlphaOfFrame = ImGui.GetStyle().Alpha;
        if (body is null || steps is not { Count: > 0 } || _ring is not { } ring || _sketchActive || _pad is not null || displaySize.X < displaySize.Y)
            return;

        // The nodes are on the band of the ring, between its two guide circles, from the lower left down to the bottom, as in
        // the render "Mix 1"; the tools of a body are on the right side of the band, and the chips inside the inner circle. The
        // nodes are a fifth of the eighth of a turn apart, so six fill it; at a small radius they are 4 pixels more than a node
        // apart, and a node past the bottom does not show.
        var radius = ring.Inner + (RingBand / 2f);
        var (upper, lower) = (MathF.PI * 0.75f, MathF.PI / 2f);
        var step = Math.Max((upper - lower) / (HistorySteps.Most - 1), (HistoryNode + 4f) / radius);
        Vector2 At(float angle) => ring.Centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        var lineHeight = ImGui.GetTextLineHeight();
        var left = 16f + _inset.X;
        // ADR-0123 item 1 (TASK-0350): the arc draws the six newest steps; a step after them is the sketch that made the body.
        for (var i = 0; i < Math.Min(steps.Count, HistorySteps.Most); i++)
        {
            var angle = upper - (i * step);
            if (angle < lower - 0.001f)
                break;
            var centre = At(angle);

            // The text is at the left of the node, out of the body. When it has no room there, or it is on the rows of the body,
            // it is at the right of the node.
            var width = ImGui.CalcTextSize(HistoryText(steps[i])).X;
            var text = new Vector4(centre.X - (HistoryNode / 2f) - HistoryTextGap - width, centre.Y - (lineHeight / 2f), centre.X - (HistoryNode / 2f) - HistoryTextGap, centre.Y + (lineHeight / 2f));
            var onRows = _rowsArea is { } rows && text.X < rows.Z && rows.X < text.Z && text.Y < rows.W && rows.Y < text.W;
            if (text.X < left || onRows)
                text = new Vector4(centre.X + (HistoryNode / 2f) + HistoryTextGap, text.Y, centre.X + (HistoryNode / 2f) + HistoryTextGap + width, text.W);
            _historyNodes.Add((steps[i], centre, text));
        }

        // Item 2: a press and a release on one node, with no window, no chip and no row under the pointer and no pad open.
        // The chips of the last frame are drawn over the arc, so a release on a chip is the tap of the chip only.
        var io = ImGui.GetIO();
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease() && ChipAt(io.MousePos) is null && RowAt(io.MousePos) is null
            && HistoryAt(io.MousePos) is { } released && HistoryAt(io.MouseClickedPos[0]) is { } pressed && pressed.Id == released.Id)
        {
            if (released.Tap == HistoryTap.EditSketch)
                _actions.Add(new EditSketchAction(released.Id));
            else if (released.Tap == HistoryTap.SelectBody)
                _actions.Add(new SelectBodyAction(released.Id));
        }

        // ADR-0119 item 3 (phase R18e, TASK-0346): the look of the render: the arc in the accent, 2 pixels wide, over two wider
        // strokes of the accent with less alpha, its glow; each node a dot on the arc: the selected body a filled dot with a
        // glow, each other step a ring of the accent with the ground of the scene; a fault in red. The target of a tap stays
        // the circle of 44 pixels. Until TASK-0346 the arc was faint and each node a round button of 44 pixels with an icon.
        var list = ImGui.GetBackgroundDrawList();
        var colours = ImGui.GetStyle().Colors;
        foreach (var (width, alpha) in new[] { (10f, 0.08f), (5f, 0.18f), (2f, 1f) })
        {
            list.PathArcTo(ring.Centre, radius, upper, lower, 48);
            list.PathStroke(AccentAlpha(alpha), ImDrawFlags.None, width);
        }

        var ink = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
        var quiet = ImGui.GetColorU32(colours[(int)ImGuiCol.TextDisabled]);
        foreach (var (shown, centre, text) in _historyNodes)
        {
            var hovered = shown.Tap != HistoryTap.None && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) && HistoryAt(io.MousePos)?.Id == shown.Id;
            var selected = shown.Id == body.Id;
            var mark = shown.Fault ? ImGui.GetColorU32(FailedColour) : AccentAlpha(1f);
            if (hovered)
                list.AddCircleFilled(centre, HistoryNode / 2f, ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), 32);
            if (selected)
            {
                list.AddCircleFilled(centre, HistoryDotSelected + 6f, shown.Fault ? ImGui.GetColorU32(FailedColour & 0x40FFFFFF) : AccentAlpha(0.25f), 32);
                list.AddCircleFilled(centre, HistoryDotSelected, mark, 32);
            }
            else
            {
                list.AddCircleFilled(centre, HistoryDot, ImGui.GetColorU32(Theme.Plate(1f)), 24);
                list.AddCircle(centre, HistoryDot, mark, 24, 1.5f);
            }

            // The text, and the state of a failed or an unresolved step after it in red, as the rows say it.
            var words = shown.Text;
            list.AddText(new Vector2(text.X, text.Y), shown.State == Engine.Core.Queries.GetFeaturesQueryHandler.Consumed ? quiet : ink, words);
            // Finding V170 of the codebase review of 2026-11-12 (TASK-0350): the red goes through GetColorU32, so it fades with
            // the arc (ADR-0116 item 2); until TASK-0350 it kept its full alpha during a gesture.
            if (shown.Fault)
                list.AddText(new Vector2(text.X + ImGui.CalcTextSize(words).X, text.Y), ImGui.GetColorU32(FailedColour), HistoryState(shown));
            _items[HistoryItem(shown.Id)] = new Vector4(centre.X - (HistoryNode / 2f), centre.Y - (HistoryNode / 2f), centre.X + (HistoryNode / 2f), centre.Y + (HistoryNode / 2f));
            _items[HistoryTextItem(shown.Id)] = text;
        }

        var caption = Spaced("HISTORY");
        var size = ImGui.CalcTextSize(caption);
        var end = At(lower);
        // The caption is at the left of the lower end of the arc, under the text of its node, as in the render.
        var place = new Vector2(end.X - (HistoryNode / 2f) - HistoryTextGap - size.X, end.Y + (HistoryNode / 2f) + 6f);
        list.AddText(place, quiet, caption);
        _items[HistoryCaptionItem] = new Vector4(place.X, place.Y, place.X + size.X, place.Y + size.Y);
    }

    // The whole text of a step: its text, and the state of a fault.
    private static string HistoryText(HistoryStep step) => step.Fault ? step.Text + HistoryState(step) : step.Text;

    private static string HistoryState(HistoryStep step) => $" · {step.State}{(step.Code is { } code ? $" {code}" : string.Empty)}";

}
