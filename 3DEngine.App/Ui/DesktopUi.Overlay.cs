using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// Phase R15 (TASK-0234): the overlays of the scene, the sketches (ADR-0034) and the dimensions (ADR-0056), a part
// of DesktopUi. Until TASK-0234 it was in DesktopUi.cs; no line changed in the split.
internal sealed unsafe partial class DesktopUi
{
    // ADR-0033 item 13: the colour of the lines and the points of a sketch, as ImGui packs
    // it (alpha, blue, green, red), and their sizes in pixels of the window.
    internal const uint SketchColour = 0xFF30C8F0;
    private const float SketchLineWidth = 2f;
    private const float SketchPointRadius = 4f;

    // ADR-0034 item 9: a selected item in white and wider, and each sketch that is not active in grey.
    internal const uint SelectedColour = 0xFFFFFFFF;

    // The mark of a snap: a square of 12 pixels in cyan (ABGR).
    internal const uint SnapColour = 0xFFFFE000;
    private const float SnapHalfSize = 6f;
    internal const uint OtherSketchColour = 0xFF909090;

    // ADR-0053 item 8: a line or a point of a reference of the sketch, dim.
    internal const uint ReferenceColour = 0xFF706048;
    private const float SelectedLineWidth = 4f;
    private const float SelectedPointRadius = 7f;
    private const float RingRadius = 12f;

    // ADR-0033 item 13: the sketches, read only, on the background list of ImGui, so they
    // come over the scene and under each window of the panel.
    private static void Sketches(SketchOverlay.Strokes sketches)
    {
        var list = ImGui.GetBackgroundDrawList();
        foreach (var line in sketches.Lines)
            list.AddLine(line.A, line.B, Colour(line.Mark), line.Mark == SketchOverlay.Mark.Selected ? SelectedLineWidth : SketchLineWidth);
        foreach (var point in sketches.Points)
            list.AddCircleFilled(point.At, point.Mark == SketchOverlay.Mark.Selected ? SelectedPointRadius : SketchPointRadius, Colour(point.Mark));
        foreach (var ring in sketches.Rings)
            list.AddCircle(ring, RingRadius, SketchColour, 0, SketchLineWidth);
        // ADR-0041 item 6 (phase R11b): a square at the point that a tap under the mouse would take.
        if (sketches.Snap is { } snap)
            list.AddRect(snap - new Vector2(SnapHalfSize), snap + new Vector2(SnapHalfSize), SnapColour, 0, ImDrawFlags.None, SketchLineWidth);
        foreach (var label in sketches.Labels)
            list.AddText(label.At, SketchColour, label.Text);
    }

    // ADR-0056 item 4: the dimension lines in orange, and those of the selected body in the colour of the selection, on
    // the background list, under each window of the panel.
    internal const uint DimensionColour = 0xFF40A0FF;
    private const float DimensionLineWidth = 1.5f;
    private const float ArrowLength = 12f;
    private const float ArrowHalfWidth = 5f;

    // V79 (TASK-0181): the labels of the last frame where the placement put them, and the size of a text in the font of
    // the panel, for a test.
    internal IReadOnlyList<DimensionOverlay.Label> PlacedLabels { get; private set; } = [];

    internal Vector2 TextSize(string text)
    {
        ImGui.SetCurrentContext(_context);
        return ImGui.CalcTextSize(text);
    }

    private void Dimensions(DimensionOverlay.Strokes dimensions, Vector2 displaySize)
    {
        var list = ImGui.GetBackgroundDrawList();
        foreach (var line in dimensions.Lines)
        {
            var colour = line.Mark == DimensionOverlay.Mark.Selected ? SelectedColour : DimensionColour;
            list.AddLine(line.A, line.B, colour, DimensionLineWidth);
            var along = line.B - line.A;
            if (!line.Arrows || !(along.Length() > 2 * ArrowLength))
                continue;
            var unit = Vector2.Normalize(along);
            var side = new Vector2(-unit.Y, unit.X) * ArrowHalfWidth;
            list.AddTriangleFilled(line.A, line.A + (unit * ArrowLength) + side, line.A + (unit * ArrowLength) - side, colour);
            list.AddTriangleFilled(line.B, line.B - (unit * ArrowLength) + side, line.B - (unit * ArrowLength) - side, colour);
        }

        // Finding V79 of the codebase review of 2026-11-05: the labels go apart, with the size of the text of this font.
        PlacedLabels = DimensionOverlay.Place(dimensions.Labels, text => ImGui.CalcTextSize(text), displaySize);
        foreach (var label in PlacedLabels)
            list.AddText(label.At, label.Mark == DimensionOverlay.Mark.Selected ? SelectedColour : DimensionColour, label.Text);
    }

    private static uint Colour(SketchOverlay.Mark mark) => mark switch
    {
        SketchOverlay.Mark.Selected => SelectedColour,
        SketchOverlay.Mark.Other => OtherSketchColour,
        SketchOverlay.Mark.Reference => ReferenceColour,
        _ => SketchColour,
    };
}
