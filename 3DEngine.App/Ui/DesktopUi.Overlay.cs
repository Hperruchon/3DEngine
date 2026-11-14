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

    // The mark of a snap: a square of 12 pixels (ABGR). ADR-0078 item 1 (TASK-0246): magenta, the one hue that the chrome,
    // the sketch and the axes do not use; until TASK-0246 it was cyan.
    internal const uint SnapColour = Theme.Snap;
    private const float SnapHalfSize = 6f;
    internal const uint OtherSketchColour = Theme.OtherSketch;

    // ADR-0053 item 8: a line or a point of a reference of the sketch, dim.
    internal const uint ReferenceColour = Theme.Reference;
    private const float SelectedLineWidth = 4f;
    private const float SelectedPointRadius = 7f;
    private const float RingRadius = 12f;

    // ADR-0075 item 3 (phase R12a, TASK-0236): the outline of the selected face, in the colour and the width of a
    // selected item, on the background list, over the scene and under each window of the panel.
    private static void FaceOutline(FaceLine face)
    {
        var list = ImGui.GetBackgroundDrawList();
        foreach (var (a, b) in face.Outline)
            Cased(list, a, b, SelectedColour, SelectedLineWidth);

        // ADR-0077 item 4 (phase R12b, TASK-0245): the selected edge over the outline, wider.
        foreach (var (a, b) in face.Edge ?? [])
            Cased(list, a, b, SelectedColour, SelectedEdgeWidth);
    }

    // ADR-0078 item 2 (TASK-0246): a mark of the viewport over a dark casing of 2 pixels on each side, so that it holds
    // its contrast on each face and on the clear.
    private static void Cased(ImDrawListPtr list, Vector2 a, Vector2 b, uint colour, float width)
    {
        list.AddLine(a, b, Theme.Casing, width + (2 * Theme.CasingWidth));
        list.AddLine(a, b, colour, width);
    }

    private const float SelectedEdgeWidth = 8f;

    // ADR-0033 item 13: the sketches, read only, on the background list of ImGui, so they
    // come over the scene and under each window of the panel.
    // ADR-0120 item 3 (phase R18f, TASK-0347): the item of the label of the plate and of its caption, for a test.
    public const string PlateLabelItem = "Plate:label";
    public const string PlateCaptionItem = "Plate:caption";
    internal const string PlateCaption = "SKETCH · UNDER THE BODY IT MAKES";

    // ADR-0120 item 3: the plate of the active sketch, as the render: a fill and an edge of the accent with a glow, under the
    // lines of the sketch.
    private static void Plate(SketchOverlay.Strokes sketches, Engine.Core.Queries.SketchInfo? active)
    {
        if (sketches.Plate is not { Count: 4 } corners || active is null)
            return;
        var list = ImGui.GetBackgroundDrawList();
        var points = corners.ToArray();
        list.AddConvexPolyFilled(ref points[0], points.Length, PlateAccent(0.10f));
        foreach (var (width, alpha) in new[] { (7f, 0.12f), (1.5f, 0.9f) })
        {
            for (var i = 0; i < 4; i++)
                list.AddLine(points[i], points[(i + 1) % 4], PlateAccent(alpha), width);
        }
    }

    // ADR-0120 item 3: over the lines of the sketch, under the plate and left of the tools of the ring at the right: the name and
    // the state of the sketch in capitals in the accent, on the ground of the scene, and, while the sketch is under its body,
    // the caption under them. Both end at the middle of the plate. The render puts the name along the far edge of the plate;
    // there the values on the edges and the body cover it, so it is under the plate with the caption.
    private void PlateText(SketchOverlay.Strokes sketches, Engine.Core.Queries.SketchInfo? active)
    {
        if (sketches.Plate is not { Count: 4 } corners || active is null)
            return;
        var list = ImGui.GetBackgroundDrawList();
        var bottom = corners.Max(point => point.Y);
        var middle = (corners.Min(point => point.X) + corners.Max(point => point.X)) / 2f;
        var left = 16f + _inset.X;

        var label = $"{SketchTitle(active).ToUpperInvariant()} · {SketchState(active).ToUpperInvariant()}";
        var size = ImGui.CalcTextSize(label);
        var at = new Vector2(Math.Max(left, middle - size.X), bottom + 12f);
        list.AddRectFilled(at - new Vector2(6f, 3f), at + size + new Vector2(6f, 3f), Theme.Plate(0.85f), 4f);
        list.AddText(at, PlateAccent(0.95f), label);
        _items[PlateLabelItem] = new Vector4(at.X, at.Y, at.X + size.X, at.Y + size.Y);
        if (sketches.Under)
        {
            var caption = Spaced(PlateCaption);
            var extent = ImGui.CalcTextSize(caption);
            var place = new Vector2(Math.Max(left, middle - extent.X), at.Y + size.Y + 8f);
            list.AddText(place, ImGui.GetColorU32(ImGuiCol.TextDisabled), caption);
            _items[PlateCaptionItem] = new Vector4(place.X, place.Y, place.X + extent.X, place.Y + extent.Y);
        }
    }

    private static uint PlateAccent(float alpha) => (Theme.Accent & 0x00FFFFFFu) | ((uint)(alpha * 255f) << 24);

    private static void Sketches(SketchOverlay.Strokes sketches)
    {
        var list = ImGui.GetBackgroundDrawList();
        foreach (var line in sketches.Lines)
            Cased(list, line.A, line.B, Colour(line.Mark), line.Mark == SketchOverlay.Mark.Selected ? SelectedLineWidth : SketchLineWidth);
        foreach (var point in sketches.Points)
        {
            var radius = point.Mark == SketchOverlay.Mark.Selected ? SelectedPointRadius : SketchPointRadius;
            list.AddCircleFilled(point.At, radius + Theme.CasingWidth, Theme.Casing);
            list.AddCircleFilled(point.At, radius, Colour(point.Mark));
        }
        foreach (var ring in sketches.Rings)
        {
            list.AddCircle(ring, RingRadius, Theme.Casing, 0, SketchLineWidth + (2 * Theme.CasingWidth));
            list.AddCircle(ring, RingRadius, SketchColour, 0, SketchLineWidth);
        }
        // ADR-0041 item 6 (phase R11b): a square at the point that a tap under the mouse would take.
        if (sketches.Snap is { } snap)
        {
            list.AddRect(snap - new Vector2(SnapHalfSize), snap + new Vector2(SnapHalfSize), Theme.Casing, 0, ImDrawFlags.None, SketchLineWidth + (2 * Theme.CasingWidth));
            list.AddRect(snap - new Vector2(SnapHalfSize), snap + new Vector2(SnapHalfSize), SnapColour, 0, ImDrawFlags.None, SketchLineWidth);
        }
        foreach (var label in sketches.Labels)
            list.AddText(label.At, SketchColour, label.Text);
    }

    // ADR-0056 item 4: the dimension lines on the background list, under each window of the panel.
    // DimensionColour stays the colour of a pending value and of a proposal (ADR-0125 item 1).
    internal const uint DimensionColour = 0xFF40A0FF;
    private const float DimensionLineWidth = 1.5f;
    private const float ArrowLength = 12f;
    private const float ArrowHalfWidth = 5f;

    // ADR-0125 item 1 (phase R20c, TASK-0352): the dimensions of the selected body have the colour of the selection, the
    // colour of its edges in the scene (SceneRenderer.SelectedEdgeColour, 1.0, 0.55, 0.1), and the overall dimensions have
    // a neutral grey, the colour of a quiet text of the chrome (#8a939f). Until TASK-0352 the overall dimensions were
    // orange, near the colour of the selection, and those of the selected body were white.
    internal const uint SelectionDimensionColour = 0xFF1A8CFF;
    internal const uint OverallDimensionColour = 0xFF9F938A;

    // ADR-0125 item 2: the word before each overall value, and the widest name of the selected body on a plate, in pixels.
    internal const string OverallWord = "Overall";
    internal const float DimensionNameWidth = 180f;

    // V79 (TASK-0181): the labels of the last frame where the placement put them, and the size of a text in the font of
    // the panel, for a test.
    internal IReadOnlyList<DimensionOverlay.Label> PlacedLabels { get; private set; } = [];

    // ADR-0125 items 3 and 4: the plate and the dimension line of each dimension of this frame, for a test.
    private List<(string Text, DimensionOverlay.Mark Mark, Vector2 From, Vector2 To, Vector4 Plate)> _dimensionPlates = [];

    internal IReadOnlyList<(string Text, DimensionOverlay.Mark Mark, Vector2 From, Vector2 To, Vector4 Plate)> DimensionPlates => _dimensionPlates;

    internal Vector2 TextSize(string text)
    {
        ImGui.SetCurrentContext(_context);
        return ImGui.CalcTextSize(text);
    }

    // ADR-0125 item 3: the size of the plate of a text: the text in the mono font of a value at the size of the panel, and the
    // padding of a pill. It asks the font, not the stack of fonts, so a test can call it outside a frame.
    internal Vector2 PlateSize(string text)
    {
        ImGui.SetCurrentContext(_context);
        return _monoFont.CalcTextSizeA(FontSize, float.MaxValue, 0f, text) + (PillPadding * 2f);
    }

    // ADR-0125 item 2: the text of a plate: "Overall · X 1200 mm" for the overall box, and the name of the selected body, or
    // its kind and a short identifier as its row, before the value of its box.
    private string PlateText(DimensionOverlay.Measured measured, LiveBody? selected)
    {
        if (measured.Mark == DimensionOverlay.Mark.Overall || selected is null)
            return $"{OverallWord} · {measured.Text}";
        var name = selected.Name ?? $"{selected.Kind} {selected.Id.ToString()[..8]}";
        return $"{FitWith(_monoFont, FontSize, name, DimensionNameWidth)} · {measured.Text}";
    }

    // ADR-0125 item 4: the controls that a plate must not go under: the fixed controls with the status line, the panels,
    // the rows, the icons, the labels and the caption of the ring, and the nodes and the texts of the history arc.
    // Finding V173 of the codebase review of 2026-11-14 (TASK-0356): the window of the live sketches, the card of a proposal,
    // the window of the images and the list of the hidden bodies come after the dimensions in the frame, so the covers hold
    // the panels and the fixed rectangles of the last frame too, as the ring does.
    private List<Vector4> DimensionCovers()
    {
        var covers = new List<Vector4>(_fixed);
        covers.AddRange(_lastFixed);
        covers.AddRange(_panels);
        covers.AddRange(_lastPanels);
        if (_rowsArea is { } rows)
            covers.Add(rows);
        // The items of the ring ("Ring", "RingText", "RingIcon") and of the history arc ("History", "HistoryText", its caption)
        // hold the rectangles of their icons, labels, nodes and texts.
        covers.AddRange(_items
            .Where(item => item.Key.StartsWith("Ring", StringComparison.Ordinal) || item.Key.StartsWith("History", StringComparison.Ordinal))
            .Select(item => item.Value));
        covers.AddRange(_ringTargets?.Values ?? Enumerable.Empty<Vector4>());
        // Finding V179 of the codebase review of 2026-11-14 (b) (TASK-0362): the arc of a proposal on a landscape display is no
        // window, so its targets, its caption and the plate of its lines of the last frame are covers too.
        covers.AddRange(_lastProposalCovers);
        return covers;
    }

    // ADR-0125 items 1 to 4 (phase R20c, TASK-0352): each dimension as a value on an edge: a plate outside the outline of its
    // box, the dimension line through the plate, parallel to the measured edge, with an arrow at each end, and an extension
    // line from each end of the edge. The frame draws them after the ring and the history arc, so that the placement knows
    // their rectangles, but on the first channel of the background list, under them.
    private void Dimensions(DimensionOverlay.Strokes dimensions, Vector2 displaySize, LiveBody? selected)
    {
        _dimensionPlates = [];
        var list = ImGui.GetBackgroundDrawList();
        var measures = dimensions.Measures;
        var texts = measures.Select(measured => PlateText(measured, selected)).ToList();
        var labels = new List<DimensionOverlay.Label>(measures.Count);
        for (var i = 0; i < measures.Count; i++)
        {
            var line = measures[i].Line;
            var plate = PlateSize(texts[i]);
            labels.Add(new DimensionOverlay.Label(EdgeValues.Placed(line) + (line.Normal * Half(plate, line.Normal)) - (plate / 2f), texts[i], measures[i].Mark));
        }

        // Finding V79 of the codebase review of 2026-11-05: the labels go apart, with the size of their plates.
        // ADR-0125 item 4: the plates of the selected body are placed first, so they stay nearest to their body, and the
        // plates of the whole move when they meet them. PlacedLabels keeps the order of the measures.
        var order = Enumerable.Range(0, measures.Count).OrderBy(i => measures[i].Mark == DimensionOverlay.Mark.Selected ? 0 : 1).ToList();
        var placed = DimensionOverlay.Place([.. order.Select(i => labels[i])], PlateSize, displaySize, DimensionCovers());
        var inOrder = new DimensionOverlay.Label[measures.Count];
        for (var k = 0; k < order.Count; k++)
            inOrder[order[k]] = placed[k];
        PlacedLabels = inOrder;

        for (var i = 0; i < measures.Count; i++)
        {
            var (label, line) = (PlacedLabels[i], measures[i].Line);
            var colour = Ink(label.Mark);
            var plate = PlateSize(label.Text);
            var (min, max) = (label.At, label.At + plate);
            var middle = (min + max) / 2f;

            // ADR-0125 item 3: the dimension line goes through the middle of the plate, so it follows a plate that the
            // placement moves along the normal. A leader goes from its point to the plate.
            Vector2 from, to;
            if (line.Leader)
            {
                (from, to) = (line.A + (line.Normal * ExtensionGap), middle);
                Cased(list, from, to, colour, DimensionLineWidth);
            }
            else
            {
                var shift = Vector2.Dot(middle - ((line.A + line.B) / 2f), line.Normal);
                (from, to) = (line.A + (line.Normal * shift), line.B + (line.Normal * shift));
                Cased(list, line.A + (line.Normal * ExtensionGap), from + (line.Normal * ExtensionPast), colour, DimensionLineWidth);
                Cased(list, line.B + (line.Normal * ExtensionGap), to + (line.Normal * ExtensionPast), colour, DimensionLineWidth);
                Cased(list, from, to, colour, DimensionLineWidth);
                var along = to - from;
                if (along.Length() > 2 * ArrowLength)
                {
                    var unit = Vector2.Normalize(along);
                    var side = new Vector2(-unit.Y, unit.X) * ArrowHalfWidth;
                    list.AddTriangleFilled(from, from + (unit * ArrowLength) + side, from + (unit * ArrowLength) - side, colour);
                    list.AddTriangleFilled(to, to - (unit * ArrowLength) + side, to - (unit * ArrowLength) - side, colour);
                }
            }

            _dimensionPlates.Add((label.Text, label.Mark, from, to, new Vector4(min.X, min.Y, max.X, max.Y)));
        }

        // ADR-0125 item 3: the plate is the pill of a value on an edge, with the ground of the scene and an outline of the colour
        // of its box; it takes no tap. The plates come after each line, so no line crosses a plate, and the plates of the
        // selected body come last, over those of the whole.
        foreach (var (text, mark, _, _, plate) in _dimensionPlates.OrderBy(entry => entry.Mark == DimensionOverlay.Mark.Selected ? 1 : 0))
        {
            var (min, max) = (new Vector2(plate.X, plate.Y), new Vector2(plate.Z, plate.W));
            var round = (max.Y - min.Y) / 2f;
            list.AddRectFilled(min, max, Theme.Plate(0.85f), round);
            list.AddRect(min, max, Ink(mark), round, ImDrawFlags.None, 1.5f);
            list.AddText(_monoFont, FontSize, min + PillPadding, Ink(mark), text);
        }

        static uint Ink(DimensionOverlay.Mark mark) => mark == DimensionOverlay.Mark.Selected ? SelectionDimensionColour : OverallDimensionColour;
    }

    private static uint Colour(SketchOverlay.Mark mark) => mark switch
    {
        SketchOverlay.Mark.Selected => SelectedColour,
        SketchOverlay.Mark.Other => OtherSketchColour,
        SketchOverlay.Mark.Reference => ReferenceColour,
        _ => SketchColour,
    };
}
