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
    // The diameter of a node: the 44 pixels of a target of ADR-0027 item 6.
    public const float HistoryNode = ValueSize;

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
        for (var i = 0; i < steps.Count; i++)
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

        // The look of the ring: a faint arc, a light ground and an outline for each node, the colour of an active
        // control for the node of the selected body, and the dim caption in spaced capitals.
        var list = ImGui.GetBackgroundDrawList();
        var colours = ImGui.GetStyle().Colors;
        var faint = ImGui.GetColorU32(colours[(int)ImGuiCol.Border] with { W = 0.35f });
        list.PathArcTo(ring.Centre, radius, upper, lower, 48);
        list.PathStroke(faint, ImDrawFlags.None, 1.5f);
        var ink = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
        var quiet = ImGui.GetColorU32(colours[(int)ImGuiCol.TextDisabled]);
        foreach (var (shown, centre, text) in _historyNodes)
        {
            var hovered = shown.Tap != HistoryTap.None && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) && HistoryAt(io.MousePos)?.Id == shown.Id;
            var selected = shown.Id == body.Id;
            var shade = colours[(int)(selected ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button)];
            list.AddCircleFilled(centre, HistoryNode / 2f, ImGui.GetColorU32(selected ? shade : shade with { W = hovered ? 0.7f : 0.35f }), 32);
            list.AddCircle(centre, HistoryNode / 2f, shown.Fault ? FailedColour : ImGui.GetColorU32(colours[(int)ImGuiCol.Border]), 32, 1.5f);
            if (HistoryIcon(shown) is { } icon)
                RingIcon(list, icon, centre, ink);

            // The text, and the state of a failed or an unresolved step after it in red, as the rows say it.
            var words = shown.Text;
            list.AddText(new Vector2(text.X, text.Y), shown.State == Engine.Core.Queries.GetFeaturesQueryHandler.Consumed ? quiet : ink, words);
            if (shown.Fault)
                list.AddText(new Vector2(text.X + ImGui.CalcTextSize(words).X, text.Y), FailedColour, HistoryState(shown));
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

    // The icon of a node: the icon of the ring for the same tool, or none.
    private static string? HistoryIcon(HistoryStep step) => step.Kind switch
    {
        "CreateSketch" => SketchOnFaceButtonLabel,
        "Extrude" => SetDistanceLabel,
        "Translate" => MoveLabel,
        _ => null,
    };
}
