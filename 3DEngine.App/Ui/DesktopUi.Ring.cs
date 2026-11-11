using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Core.Sketching;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0081 (the redesign, slice b2, TASK-0253): the tool ring of the "mix" direction of the designer brief. While a body
// is selected, its tools are the segments of a ring: on a landscape display to the right of the body, around the centre
// of its box on the screen; on a portrait display, as the phone, in the lower right corner, under the thumb. The ring
// holds the tools that exist today and need no text: Move, Distance for an extrude, Hide or Show, and Sketch on the
// selected face. The band of the ring is a part of the panels for a tap; the inside of the ring is the scene. While a sketch
// is active, the ring holds the tools of the sketch, Constrain and Finish (ADR-0086, ADR-0087), and the second ring of the
// constraints that fit the selection; finding V138 of the codebase review of 2026-11-07 (c) (TASK-0282) added this sentence.
internal sealed unsafe partial class DesktopUi
{
    // The depth of a segment, from the inner to the outer radius (size-ring-band of the design system).
    public const float RingBand = 68f;

    // The angle of one segment, in radians, and the gap between two segments.
    internal const float RingSegment = 0.42f;
    private const float RingGap = 0.04f;

    // ADR-0119 item 2 (phase R18e, TASK-0346): the largest span of the segments of a landscape ring: the span of four segments,
    // about 105 degrees, so the top segment ends before Accept of a proposal at 305 degrees and the bottom one above the history
    // arc.
    internal const float RingSpan = (4 * RingSegment) + (3 * RingGap);

    // One segment of the ring: its label, its angles from the positive x axis of the screen (y down), and its action.
    internal sealed record RingSegmentLayout(string Label, float From, float To);

    // The place and the size of the ring for a display: its centre, its inner radius, the angle of its first segment, and
    // the angle of one segment.
    internal sealed record RingLayout(Vector2 Centre, float Inner, float Start, float Direction, float Segment = RingSegment)
    {
        public float Outer => Inner + RingBand;

        // The segments, in order, from the start in the direction of the layout.
        public IReadOnlyList<RingSegmentLayout> Segments(IReadOnlyList<string> labels)
        {
            var list = new List<RingSegmentLayout>();
            for (var i = 0; i < labels.Count; i++)
            {
                var a = Start + (Direction * i * (Segment + RingGap));
                var b = a + (Direction * Segment);
                list.Add(new RingSegmentLayout(labels[i], Math.Min(a, b), Math.Max(a, b)));
            }

            return list;
        }

        // The segment under a point of the window, or null.
        public string? At(IReadOnlyList<string> labels, Vector2 point)
        {
            var d = point - Centre;
            var r = d.Length();
            if (r < Inner || r > Outer)
                return null;
            var angle = MathF.Atan2(d.Y, d.X);
            foreach (var segment in Segments(labels))
            {
                foreach (var turn in new[] { -MathF.Tau, 0f, MathF.Tau })
                {
                    if (angle + turn >= segment.From && angle + turn <= segment.To)
                        return segment.Label;
                }
            }

            return null;
        }

        // The middle of a segment, where its label goes.
        public Vector2 Middle(RingSegmentLayout segment)
        {
            var angle = (segment.From + segment.To) / 2f;
            var radius = (Inner + Outer) / 2f;
            return Centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }
    }

    // The layout for a display and the centre of the selected body on the screen. Landscape: the ring is around the body,
    // with an inner radius of a third of the height (size-ring-inner: 240 pixels at 720, 375 at 1080), and its segments
    // start above the right side and go down. Portrait: the thumb ring, in the lower right corner, with its segments from
    // the left up to the top.
    // Findings V121 and V122 of the codebase review of 2026-11-07 (TASK-0264): the portrait segments fit between the left
    // and the top for each count, narrower when they must be (four give 0.36 radians, 54 pixels at the inner radius); until
    // TASK-0264 the fourth one went past the top and out of the display. The corner is 16 pixels inside the safe area
    // (left, top, right and bottom), so the navigation bar of the phone does not cover Move.
    // Finding V136 of the codebase review of 2026-11-07 (c) (TASK-0281): the portrait corner is 8 pixels above the top of the
    // anchors (anchorsTop), so no segment is under Frame; until TASK-0281 the first segment, Line or Move, was under it at 390
    // by 844.
    internal static RingLayout Layout(Vector2 display, Vector2 centre, int count, float body = 0f, Vector4 inset = default, float anchorsTop = float.MaxValue)
    {
        var span = (count * RingSegment) + ((count - 1) * RingGap);
        if (display.X < display.Y)
        {
            var segment = Math.Min(RingSegment, ((MathF.PI / 2f) - ((count - 1) * RingGap)) / Math.Max(count, 1));
            // ADR-0086 item 3 (TASK-0275): the inner radius grows when a narrow segment would be less than 46 pixels at it
            // (44 and a margin of 2), as the five tools of a sketch need (0.28 radians).
            var portraitInner = Math.Max(150f, (ValueSize + 2f) / segment);
            return new RingLayout(new Vector2(display.X - 16f - inset.Z, Math.Min(display.Y - 16f - inset.W, anchorsTop - 8f)), portraitInner, MathF.PI, 1f, segment);
        }

        // The ring is outside the body on the screen: at least the half of the diagonal of its box, and 24 pixels more.
        // ADR-0113 item 5 (phase R19, TASK-0336): the inner radius is at most the half of the height between the top of the
        // display and 8 pixels above the anchors, less the band and 16 pixels, and at least 100, as the ring of a sketch
        // (V134). A ring that fits keeps its radius. Until TASK-0336 the ring was at least 160 at each height, and at 640 by
        // 360 the status line covered Hide and "Sketch on face" went below the display.
        var height = Math.Min(display.Y - inset.W, anchorsTop - 8f) - inset.Y;
        var inner = Math.Clamp(Math.Max(display.Y / 3f, body + 24f), 160f, 375f);
        inner = Math.Min(inner, Math.Max(100f, (height / 2f) - RingBand - 16f));
        var reach = inner + RingBand + 16f;
        var clamped = new Vector2(Math.Clamp(centre.X, reach, Math.Max(reach, display.X - reach)), Math.Clamp(centre.Y, reach, Math.Max(reach, display.Y - reach)));
        // ADR-0119 item 2 (phase R18e, TASK-0346): the segments of a landscape ring span at most RingSpan; more than four segments
        // are narrower, and each keeps at least 46 pixels at the inner radius.
        var landscapeSegment = Math.Min(RingSegment, Math.Max((RingSpan - ((count - 1) * RingGap)) / Math.Max(count, 1), (ValueSize + 2f) / inner));
        var landscapeSpan = (count * landscapeSegment) + ((count - 1) * RingGap);
        return new RingLayout(clamped, inner, -landscapeSpan / 2f, 1f, landscapeSegment);
    }

    // ADR-0086 item 1 (TASK-0275): the ring of an active sketch on a landscape display. Its centre is the centre of the sketch
    // on the screen, and its inner radius is 24 pixels more than the half of the diagonal of the sketch, at least 100.
    // Finding V134 of the codebase review of 2026-11-07 (c) (TASK-0280): the inner radius is at most the half of the display
    // less the band and 16 pixels, so the band fits the height; until TASK-0280 a large sketch put Finish below the display.
    // ADR-0097 item 4 (the redesign, slice n, TASK-0308): the rows of the sketch are at the left, so the segments go down on
    // the right side, as the ring of a body and the render. The centre moves left only as far as the band needs to stay 16
    // pixels inside the display, and never left of the right edge of the rows, so no segment is under them. Until TASK-0308
    // the window of the sketch was at the right and the segments went down on the left side.
    internal static RingLayout SketchLayout(Vector2 display, Vector2 centre, int count, float sketch, float rowsRight)
    {
        var span = (count * RingSegment) + ((count - 1) * RingGap);
        // ADR-0120 item 2 (phase R18f, TASK-0347): the rule of the span of a body ring (ADR-0119 item 2), so the tools of a sketch
        // end before Accept of a proposal. A sketch needs each of its tools, so the inner radius grows until a narrowed segment is
        // at least 46 pixels at it, as a portrait ring does (ADR-0086 item 3), up to the half of the display.
        var target = (RingSpan - ((count - 1) * RingGap)) / Math.Max(count, 1);
        var inner = Math.Min(Math.Max(Math.Max(100f, sketch + 24f), target < RingSegment ? (ValueSize + 2f) / target : 0f), Math.Max(100f, (display.Y / 2f) - 16f - RingBand));
        var reach = inner + RingBand + 16f;
        var x = Math.Max(Math.Min(centre.X, display.X - reach), rowsRight);
        var y = Math.Clamp(centre.Y, reach, Math.Max(reach, display.Y - reach));
        var segment = Math.Min(RingSegment, Math.Max(target, (ValueSize + 2f) / inner));
        var narrowed = (count * segment) + ((count - 1) * RingGap);
        return new RingLayout(new Vector2(x, y), inner, -narrowed / 2f, 1f, segment);
    }

    // Finding V135 of the codebase review of 2026-11-07 (c) (TASK-0281): a release over a window belongs to the window. While an
    // item of a window is active, ImGui counts the window as not hovered unless the flag says so, so until TASK-0281 the ring,
    // the rows and the chips took a release over a row of a window too, and one tap gave two actions.
    private static bool OverWindowAtRelease()
        => ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

    public static string RingItem(string label) => $"Ring:{label}";

    // The rectangle of the text of the label of a segment.
    public static string RingTextItem(string label) => $"RingText:{label}";

    private RingLayout? _ring;
    private IReadOnlyList<string> _ringLabels = [];

    // ADR-0116 item 1 (phase R20a, TASK-0341): the target of a tap of each segment of a body ring of this frame, the box of its
    // icon and its label grown to at least 44 by 44 pixels; null for a ring of a sketch, whose whole band takes a tap.
    private Dictionary<string, Vector4>? _ringTargets;

    // ADR-0116 item 1: the margin of the box of the icon and the label of a segment, at each side.
    internal const float RingTargetMargin = 4f;

    // ADR-0116 item 2 (phase R20a, TASK-0341): the alpha of the ring and of the history arc while an orbit, a pan or a zoom runs.
    internal const float GestureFade = 0.2f;

    // ADR-0116 item 2: true while the host turns, pans or zooms the view (GestureFadeTimer); the host sets it before each frame.
    public bool CameraGesture { get; set; }

    // ADR-0116 item 2: the alpha of the style when the ring and the history arc of this frame were drawn, for a test.
    internal float RingAlphaOfFrame { get; private set; } = 1f;

    internal float HistoryAlphaOfFrame { get; private set; } = 1f;

    // ADR-0116 item 2: the fade of a gesture, over the dim of a waiting proposal; true when the caller must pop it.
    private bool FadeForGesture()
    {
        if (!CameraGesture)
            return false;
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, GestureFade);
        return true;
    }

    // ADR-0119 item 2 (phase R18e, TASK-0346): the layout and the labels of the ring of this frame, for a test.
    internal RingLayout? RingLayoutOfFrame => _ring;

    internal IReadOnlyList<string> RingLabels => _ringLabels;

    // ADR-0122 item 2 (phase R20b, TASK-0348): the segments of this frame that have the colour of an active control.
    private readonly HashSet<string> _ringInUse = [];

    internal bool RingInUse(string label) => _ringInUse.Contains(label);

    // ADR-0116 item 1: the target of a tap of a segment of this frame, for a test; null on a ring of a sketch.
    internal Vector4? RingTarget(string label) => _ringTargets is { } targets && targets.TryGetValue(label, out var target) ? target : null;

    // True when the point is on a segment of the ring of this frame that takes a tap there.
    private bool OverRing(double x, double y) => RingTapAt(new Vector2((float)x, (float)y)) is not null;

    // ADR-0116 item 1 (phase R20a, TASK-0341): the segment that takes a tap at a point: on a body ring only on the target of its
    // icon and its label, inside the segment; the rest of the band is the scene. Until TASK-0341 the whole band took a tap, and
    // a click on a body under it opened a tool of the selected body (problem 1 of the UX test of 2026-11-10).
    private string? RingTapAt(Vector2 point)
    {
        if (_ring is not { } ring || ring.At(_ringLabels, point) is not { } label)
            return null;
        if (_ringTargets is null)
            return label;
        return _ringTargets.TryGetValue(label, out var target)
            && point.X >= target.X && point.X < target.Z && point.Y >= target.Y && point.Y < target.W ? label : null;
    }

    // ADR-0116 item 1: the box of a face of a segment, grown by the margin and then to at least 44 by 44 pixels about its centre.
    internal static Vector4 TargetOf(Vector2 min, Vector2 max)
    {
        var (a, b) = (min - new Vector2(RingTargetMargin), max + new Vector2(RingTargetMargin));
        var centre = (a + b) / 2f;
        var half = Vector2.Max((b - a) / 2f, new Vector2(ValueSize / 2f));
        return new Vector4(centre.X - half.X, centre.Y - half.Y, centre.X + half.X, centre.Y + half.Y);
    }

    // The place of the label of a segment, the centre of its icon or null, and true when the label has a plate (ADR-0087 item 5).
    private (Vector2 Place, Vector2? Icon, bool Plate) SegmentFace(RingLayout ring, RingSegmentLayout segment, Vector2 displaySize, bool sketchRing)
    {
        var middle = ring.Middle(segment);
        var text = ImGui.CalcTextSize(RingText(segment.Label));
        // The review of Codex of v0.251 (TASK-0271): a label at the edge of a portrait display stays 4 pixels inside the
        // safe area; until TASK-0271 the label "Sketch on face" of the fourth segment went past the right edge.
        var place = middle - (text / 2f);
        place.X = Math.Clamp(place.X, 4f + _inset.X, Math.Max(4f + _inset.X, displaySize.X - _inset.Z - 4f - text.X));
        // ADR-0087 item 5 (TASK-0276): a label of a sketch ring wider than the band starts 6 pixels outside the inner edge,
        // so it covers less of the sketch; a plate of the colour of its segment keeps it readable over the lines. The first
        // form centred "Horizontal" over the points. ADR-0097 item 4 (TASK-0308): the ring is on the right of the sketch,
        // so the label goes out to the right and stays 4 pixels inside the display.
        // ADR-0097 item 4: a tool with an icon keeps its icon and its label in the middle of the segment.
        if (sketchRing && text.X > RingBand - 12f && !RingIconLabels.Contains(segment.Label))
        {
            var inner = ring.Centre.X + (MathF.Cos((segment.From + segment.To) / 2f) * ring.Inner);
            place.X = Math.Min(inner + 6f, displaySize.X - _inset.Z - 4f - text.X);
            return (place, null, true);
        }

        if (!RingIconLabels.Contains(segment.Label))
            return (place, null, false);
        var top = middle.Y - (((2f * IconHalf) + IconGap + text.Y) / 2f);
        place.Y = top + (2f * IconHalf) + IconGap;
        return (place, new Vector2(middle.X, top + IconHalf), false);
    }

    // ADR-0086 (the redesign, slice f, TASK-0275): the tools of the active sketch, in the order of the window of the sketch,
    // and the kind of each one; Finish ends the sketch.
    internal static readonly IReadOnlyList<(string Label, SketchToolKind? Kind)> SketchRingTools =
    [
        (LineToolLabel, SketchToolKind.Line),
        (SelectToolLabel, SketchToolKind.Select),
        (CircleToolLabel, SketchToolKind.Circle),
        (ArcToolLabel, SketchToolKind.Arc),
        (FinishLabel, null),
    ];

    // The tool of the active sketch in this frame, or null with no active sketch.
    private SketchToolKind? _sketchTool;

    // ADR-0087 (the redesign, slice g, TASK-0276): the segment that opens the second ring of the constraints that fit the
    // selection of the sketch, and the segment that goes back to the tools.
    public const string ConstrainLabel = "Constrain";
    public const string BackLabel = "Back";

    // The panel of the sketch of this frame; true while the second ring is open, and the selection that it belongs to.
    private SketchPanel? _sketchPanel;
    private bool _constrainOpen;
    private string _constrainKey = string.Empty;
    private List<(string Label, Action Press)> _fitting = [];

    // V134 (TASK-0280): the window of the sketch of the last frame, which the ring of this frame must not be under, and true
    // when the middle of each segment of the ring of this frame is on the display and under no window and no anchor. The
    // window of the sketch drops the buttons of the ring only then (ADR-0088 item 1).
    private Vector4? _sketchWindow;
    private bool _ringReachable;

    // The ring of the selected body, or of the active sketch (ADR-0086), or none while a pad is open or with no centre.
    private void Ring(Vector2 displaySize, LiveBody? body, Vector2? centre, float size)
    {
        _ring = null;
        _ringLabels = [];
        _ringTargets = null;
        _ringReachable = false;
        _ringInUse.Clear();
        RingAlphaOfFrame = ImGui.GetStyle().Alpha;
        if (centre is not { } at || _pad is not null || (body is null && !_sketchActive))
            return;

        var labels = new List<string>();
        if (_sketchActive)
        {
            // ADR-0086 items 1 and 2: while a sketch is active, the ring holds its tools in the place of the tools of a body.
            // ADR-0087 items 1 and 3: "Constrain" comes before Finish when a constraint fits the selection, and opens the
            // second ring of those constraints with "Back"; a change of the selection closes the second ring.
            _fitting = _sketchPanel?.Active is { } active ? FittingConstraints(active, _sketchPanel.Selection) : [];
            var key = string.Join(",", _sketchPanel?.Selection ?? []);
            if (key != _constrainKey)
                (_constrainOpen, _constrainKey) = (false, key);
            if (_constrainOpen && _fitting.Count > 0)
            {
                labels.AddRange(_fitting.Select(fit => fit.Label));
                labels.Add(BackLabel);
            }
            else
            {
                _constrainOpen = false;
                labels.AddRange(SketchRingTools.Where(tool => tool.Kind is not null).Select(tool => tool.Label));
                // ADR-0120 item 2 (phase R18f, TASK-0347): "Next point" while the tool Line is active, and "Extrude" before
                // Finish, as the window gave them until TASK-0347.
                if (_sketchTool == SketchToolKind.Line)
                    labels.Add(NextPointLabel);
                if (_fitting.Count > 0)
                    labels.Add(ConstrainLabel);
                labels.Add(ExtrudeLabel);
                labels.Add(FinishLabel);
            }
        }
        else if (body is not null)
        {
            // ADR-0119 item 1 (phase R18e, TASK-0346): "Edit sketch" first, for a body that a sketch made, as the render.
            if (SketchStep is not null)
                labels.Add(EditSketchLabel);
            // Finding V130 of the codebase review of 2026-11-07 (b) (TASK-0271): a mesh body takes no move (ADR-0083 item 4).
            if (body.Kind != Engine.Core.Commands.ImportStlCommandHandler.MeshKind)
                labels.Add(MoveLabel);
            if (body.Kind == "Extrude")
                labels.Add(SetDistanceLabel);
            labels.Add(body.Visible ? HideLabel : ShowLabel);
            // ADR-0101 item 3 (phase R17b, TASK-0315): "Focus" after Hide.
            labels.Add(FocusLabel);
            // ADR-0122 item 1 (phase R20b, TASK-0348): "Dimensions" after Focus, so a person turns the dimensions on and off with
            // no visit to the panel. On a small ring it leaves after "Rename" and "Edit sketch" (ADR-0119 item 2).
            labels.Add(DimensionsLabel);
            // ADR-0119 item 1: "Rename" after them on a landscape display, where it opens the name field of the rows; a portrait
            // display keeps the name field and its button in the panel.
            if (displaySize.X >= displaySize.Y)
                labels.Add(RenameLabel);
            if (_face is { CanSketch: true })
                labels.Add(SketchOnFaceButtonLabel);
        }

        // ADR-0097 item 4 (the redesign, slice n, TASK-0308): on a landscape display the ring of a sketch is on the right of the
        // sketch, and right of the rows of the sketch of the last frame, which are at the left in the place of the panel.
        var sketchRing = _sketchActive && displaySize.X >= displaySize.Y;

        // ADR-0119 item 2: on a landscape body ring that has no room for each tool in RingSpan, "Rename" and then "Edit sketch" leave
        // the ring; the first row of the rows renames the body, and the row "Made from" opens its sketch.
        // ADR-0122 item 1 (phase R20b, TASK-0348): then "Dimensions" leaves; the command line and the panel keep its toggle.
        if (!_sketchActive && body is not null && displaySize.X >= displaySize.Y)
        {
            foreach (var spare in new[] { RenameLabel, EditSketchLabel, DimensionsLabel })
            {
                var trial = Layout(displaySize, at, labels.Count, size, _inset, _anchorsTop);
                if ((labels.Count * trial.Segment) + ((labels.Count - 1) * RingGap) <= RingSpan + 0.001f)
                    break;
                labels.Remove(spare);
            }
        }
        var ring = sketchRing
            ? SketchLayout(displaySize, at, labels.Count, size, _sketchWindow?.Z ?? 0f)
            : Layout(displaySize, at, labels.Count, size, _inset, _anchorsTop);
        _ring = ring;
        _ringLabels = labels;
        // ADR-0116 item 1 (phase R20a, TASK-0341): the face of each segment, and on a body ring the target of its tap. A ring of a
        // sketch keeps its whole band as the target (ADR-0086).
        var faces = ring.Segments(labels).Select(segment => (Segment: segment, Face: SegmentFace(ring, segment, displaySize, sketchRing))).ToList();
        _ringTargets = _sketchActive ? null : faces.ToDictionary(entry => entry.Segment.Label, entry =>
        {
            var text = ImGui.CalcTextSize(RingText(entry.Segment.Label));
            var (min, max) = (entry.Face.Place, entry.Face.Place + text);
            if (entry.Face.Icon is { } icon)
                (min, max) = (Vector2.Min(min, icon - new Vector2(IconHalf)), Vector2.Max(max, icon + new Vector2(IconHalf)));
            return TargetOf(min, max);
        });
        // The review of Codex of v0.265 (TASK-0284): the windows of the last frame too, because the Images window and the
        // message come after the ring in the frame; until TASK-0284 a segment under the Images window counted as reachable.
        var covers = _panels.Concat(_lastPanels).Concat(_fixed).Concat(_sketchActive && _sketchWindow is { } window ? [window] : []).ToList();
        _ringReachable = ring.Segments(labels).Select(ring.Middle).All(middle =>
            middle.X >= 0f && middle.Y >= 0f && middle.X < displaySize.X && middle.Y < displaySize.Y
            && !covers.Any(cover => middle.X >= cover.X && middle.X < cover.Z && middle.Y >= cover.Y && middle.Y < cover.W));

        // The press and the release on one segment, with no window of the panels under the mouse, give its tool.
        // Finding V120 of the codebase review of 2026-11-07 (TASK-0264): a chip is drawn over the ring, so a release on a
        // chip is the tap of the chip only; the chips of the last frame are the chips that the person sees. Until
        // TASK-0264 one tap gave both tools.
        // Finding V160 of the codebase review of 2026-11-09 (b) (TASK-0331): Reject and Accept of a proposal take the tap before
        // the ring, at the press and at the release. Until TASK-0331 one tap on Accept also gave the tool of the ring under it.
        // ADR-0116 item 1: the press and the release are on the target of the segment.
        var io = ImGui.GetIO();
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease() && ChipAt(io.MousePos) is null && RowAt(io.MousePos) is null
            && !OverLastProposal(io.MousePos) && !OverLastProposal(io.MouseClickedPos[0])
            && RingTapAt(io.MousePos) is { } released && RingTapAt(io.MouseClickedPos[0]) == released)
        {
            if (_sketchActive)
                UseSketchTool(released);
            else if (body is not null)
                Use(released, body);
        }

        var list = ImGui.GetBackgroundDrawList();
        var colours = ImGui.GetStyle().Colors;
        // ADR-0095 item 1 (TASK-0306): the guide of a landscape ring, under its segments.
        var landscape = displaySize.X >= displaySize.Y;
        if (landscape)
            RingGuide(list, ring);
        foreach (var (segment, face) in faces)
        {
            // ADR-0116 item 1: a segment shows the hover only where it takes a tap.
            var hovered = RingTapAt(io.MousePos) == segment.Label && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
            // ADR-0086 item 2: the tool in use has the colour of an active control.
            // ADR-0101 item 3 (phase R17b, TASK-0315): while a focus lasts, "Focus" has the colour of an active control too.
            var inUse = (_sketchActive && SketchRingTools.FirstOrDefault(tool => tool.Label == segment.Label).Kind is { } kind && kind == _sketchTool)
                || (!_sketchActive && segment.Label == FocusLabel && Focus is not null)
                // ADR-0122 item 2: while the dimensions are on, "Dimensions" has the colour of an active control too.
                || (!_sketchActive && segment.Label == DimensionsLabel && _dimensions);
            if (inUse)
                _ringInUse.Add(segment.Label);
            // ADR-0108 item 2 (phase R18a, TASK-0332): no fill at rest, and the light fill of the theme under the pointer and
            // for the tool in use. Until TASK-0332 each segment had a blue ground at alpha 0.35 (ADR-0095 item 2).
            var ground = ImGui.GetColorU32(colours[(int)(inUse ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button)]);
            // A segment is not convex, so it is filled as a row of small quads.
            const int steps = 12;
            for (var i = 0; i < steps; i++)
            {
                var (a, b) = (segment.From + ((segment.To - segment.From) * i / steps), segment.From + ((segment.To - segment.From) * (i + 1) / steps));
                Vector2 Point(float angle, float radius) => ring.Centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
                list.AddQuadFilled(Point(a, ring.Inner), Point(a, ring.Outer), Point(b, ring.Outer), Point(b, ring.Inner), ground);
            }

            list.PathArcTo(ring.Centre, ring.Outer, segment.From, segment.To);
            list.PathArcTo(ring.Centre, ring.Inner, segment.To, segment.From);
            // ADR-0108 item 2: a thin outline; the tool in use has a wider outline in the accent, its clear mark with no fill.
            list.PathStroke(inUse ? ImGui.GetColorU32(Theme.Accent) : ImGui.GetColorU32(colours[(int)ImGuiCol.Border]), ImDrawFlags.Closed, inUse ? Theme.InUseWidth : 1.5f);

            var middle = ring.Middle(segment);
            // ADR-0095 item 3: the short label of the render, and a drawn icon above it. SegmentFace gives their places.
            var caption = RingText(segment.Label);
            var text = ImGui.CalcTextSize(caption);
            var place = face.Place;
            var ink = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
            // ADR-0108 item 2: the plate has the ground of the scene, because a segment has no fill at rest.
            if (face.Plate)
                list.AddRectFilled(place - new Vector2(4f, 2f), place + text + new Vector2(4f, 2f), Theme.Plate(0.85f), 4f);
            // ADR-0108 item 2: the icon is in the accent, as the render, with the alpha of the style, so it dims with the ring.
            else if (face.Icon is { } icon && RingIcon(list, segment.Label, icon, ImGui.GetColorU32(Theme.Accent)))
                _items[RingIconItem(segment.Label)] = new Vector4(icon.X - IconHalf, icon.Y - IconHalf, icon.X + IconHalf, icon.Y + IconHalf);

            list.AddText(place, ink, caption);
            _items[RingTextItem(segment.Label)] = new Vector4(place.X, place.Y, place.X + text.X, place.Y + text.Y);
            _items[RingItem(segment.Label)] = new Vector4(middle.X - 4f, middle.Y - 4f, middle.X + 4f, middle.Y + 4f);
        }

        // ADR-0095 item 4: the caption of a landscape body ring. ADR-0097 item 4 (TASK-0308): a sketch ring on the right has it
        // too, with the name of the sketch, as the render ("TOOLS FOR PLATE ON XY").
        if (landscape && !_sketchActive && body is not null)
            RingCaption(list, ring, body.Name ?? body.Kind, displaySize);
        else if (sketchRing && _sketchPanel?.Active is { } shown)
            RingCaption(list, ring, SketchTitle(shown), displaySize);
    }

    // ADR-0086 item 2: the tool of a segment of the ring of a sketch, as its button of the window of the sketch does.
    private void UseSketchTool(string label)
    {
        // ADR-0087 item 3: the second ring opens and closes, and a constraint does what its button of the window does.
        if (_constrainOpen)
        {
            if (label != BackLabel && _fitting.FirstOrDefault(fit => fit.Label == label).Press is { } press)
                press();
            _constrainOpen = false;
            return;
        }

        if (label == ConstrainLabel)
        {
            _constrainOpen = true;
            return;
        }

        // ADR-0120 item 2: the pad of the next point, and the pad of an extrude, as their buttons of the window did.
        if (label == NextPointLabel)
        {
            OpenNextPointPad(_sketchPanel?.ChainOpen ?? false);
            return;
        }

        if (label == ExtrudeLabel)
        {
            OpenExtrudePad();
            return;
        }

        if (SketchRingTools.FirstOrDefault(tool => tool.Label == label) is not ({ } _, var kind))
            return;
        _actions.Add(kind is { } tool ? new SketchToolAction(tool) : new FinishSketchAction());
    }

    // The tool of a segment, as its button of the panel does.
    private void Use(string label, LiveBody body)
    {
        if (label == MoveLabel)
            OpenPad(new NumericPad(MoveLabel, _unit, new("dx", "0"), new("dy", "0"), new("dz", "0")), values => new MoveBodyAction(body.Id, values[0], values[1], values[2]), scope: PadScope.Body);
        else if (label == SetDistanceLabel)
            _actions.Add(new EditDistanceAction(body.Id));
        else if (label == HideLabel || label == ShowLabel)
            _actions.Add(new SetVisibleAction(body.Id, !body.Visible));
        else if (label == SketchOnFaceButtonLabel)
            _actions.Add(new SketchOnSelectedFaceAction());
        // ADR-0101 items 3 and 5: "Focus" starts a focus on the body, or ends it.
        else if (label == FocusLabel)
            _actions.Add(new FocusAction());
        // ADR-0119 item 1: "Edit sketch" opens the sketch that made the body, as the row "Made from" does; "Rename" opens the
        // name field of the rows, as a tap on the first row does.
        else if (label == EditSketchLabel && SketchStep is { } sketch)
            _actions.Add(new EditSketchAction(sketch.Id));
        else if (label == RenameLabel)
            StartRename(body);
        // ADR-0122 item 1: "Dimensions" turns the dimensions on or off, as the toggle of the panel does.
        else if (label == DimensionsLabel)
            _dimensions = !_dimensions;
    }
}
