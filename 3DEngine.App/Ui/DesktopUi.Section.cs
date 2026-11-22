using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Numeric;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0184 (phase R26, TASK-0447): the section view. "Section" on the ring of the part starts a section on the plane XZ through
// the middle of the visible bodies; while it lasts, the ring of the part holds "Section", "Plane", "Offset" and "Flip". The
// section is a state of the client (ADR-0007): no tool of it sends an action to the host or a command to the engine. The
// renderer cuts the bodies (SectionView); the window gives it the plane only.
internal sealed unsafe partial class DesktopUi
{
    // ADR-0184 item 1: the tool that starts and ends a section, on the ring of the part, has the label SectionLabel, the word
    // of the action "Section" of the sheet page.

    // ADR-0184 items 2 and 3: the tools of the ring of the part while a section lasts.
    public const string SectionPlaneLabel = "Plane";
    public const string SectionOffsetLabel = "Offset";
    public const string SectionFlipLabel = "Flip";

    // ADR-0184 item 2: the three origin planes. The direction of each is the side of the eye of the start view (yaw 135 degrees,
    // pitch 30 degrees), so a new section shows its caps: +z for XY, +y for XZ and +x for YZ.
    internal static readonly IReadOnlyList<SectionPlane> OriginSectionPlanes =
    [
        new(Engine.Contracts.Sketch.Datums.XY.Name, (0, 0, 0), (0, 0, 1)),
        new(Engine.Contracts.Sketch.Datums.XZ.Name, (0, 0, 0), (0, 1, 0)),
        new(Engine.Contracts.Sketch.Datums.YZ.Name, (0, 0, 0), (1, 0, 0)),
    ];

    // ADR-0184 item 2: a new section starts on XZ, the plane of the front view.
    internal static SectionPlane DefaultSectionPlane => OriginSectionPlanes[1];

    private SectionState? _section;
    private IReadOnlyList<SectionPlane> _sectionDatums = [];

    // ADR-0184: the section of the window, or null; the host gives its cut to the renderer before each frame.
    internal SectionState? Section => _section;

    // ADR-0184 item 2: the box of the visible bodies, which the host gives before each frame, for the offset of a new section.
    internal ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? SectionBox { get; set; }

    // ADR-0184 item 2: the named datums of the document with no fault, which the host gives while a section lasts. TASK-0449
    // (finding V232 of the third codebase review of 2026-11-21): a section on a datum takes the plane of the same datum, by its
    // identity, so a move, a rename or an undo of the datum moves the section; it keeps its offset and its flip. A datum that is
    // gone gives the plane of a new section, XZ, with the flip.
    internal void ShowSectionDatums(IReadOnlyList<SectionPlane> datums)
    {
        ArgumentNullException.ThrowIfNull(datums);
        _sectionDatums = datums;
        if (_section is not { Plane.Datum: { } datum } shown)
            return;
        var now = datums.FirstOrDefault(plane => plane.Datum == datum);
        if (now is null)
            _section = new SectionState(DefaultSectionPlane, DefaultOffset(DefaultSectionPlane), shown.Flip);
        else if (now != shown.Plane)
            _section = shown with { Plane = now };
    }

    // ADR-0184 item 2: the planes that "Plane" goes through, in order: the origin planes, then each named datum.
    internal IReadOnlyList<SectionPlane> SectionPlanes => [.. OriginSectionPlanes, .. _sectionDatums];

    // ADR-0184 item 1: a tap on "Section" starts a section, and a second tap ends it; for a test, as the tap does.
    internal void StartSection() => _section = new SectionState(DefaultSectionPlane, DefaultOffset(DefaultSectionPlane), false);

    // ADR-0184 item 2: the offset of a new plane. An origin plane goes through the middle of the box of the visible bodies,
    // rounded to a whole unit, so the pad shows a short number; a datum is at its own place, an offset of 0.
    private double DefaultOffset(SectionPlane plane)
    {
        if (!OriginSectionPlanes.Contains(plane) || SectionBox is not { } box)
            return 0;
        var (d, min, max) = (plane.Direction, box.Min, box.Max);
        var middle = (((min.X + max.X) * d.X) + ((min.Y + max.Y) * d.Y) + ((min.Z + max.Z) * d.Z)) / 2;
        return double.IsFinite(middle) ? Math.Round(middle, MidpointRounding.AwayFromZero) : 0;
    }

    // ADR-0184 items 1 to 3: the labels of the ring of the part while a section lasts.
    private static List<string> SectionRingLabels() => [SectionLabel, SectionPlaneLabel, SectionOffsetLabel, SectionFlipLabel];

    // ADR-0184 items 1 to 3: the tool of a segment of the ring of the part that belongs to the section; false for another tool.
    private bool UseSection(string label)
    {
        switch (label)
        {
            case SectionLabel:
                if (_section is null)
                    StartSection();
                else
                    _section = null;
                return true;
            case SectionPlaneLabel when _section is { } shown:
                var planes = SectionPlanes;
                var next = planes[(planes.ToList().FindIndex(plane => plane == shown.Plane) + 1) % planes.Count];
                _section = new SectionState(next, DefaultOffset(next), shown.Flip);
                return true;
            case SectionOffsetLabel when _section is { } shown:
                OpenSectionPad(shown.Offset);
                return true;
            case SectionFlipLabel when _section is { } shown:
                _section = shown with { Flip = !shown.Flip };
                return true;
            default:
                return false;
        }
    }

    // ADR-0184 item 3: the pad of the offset, a value pad as the others in the unit of the document. Its apply sets the offset of
    // the section and closes the pad; it gives no action to the host.
    private void OpenSectionPad(double offset)
    {
        OpenPad(new NumericPad(SectionOffsetLabel, _unit, new PadField("offset", SketchText.Number(offset))), _ => throw new InvalidOperationException("The pad of a section gives no action."));
        _padLocal = values =>
        {
            if (_section is { } shown)
                _section = shown with { Offset = values[0] };
        };
    }

    // ADR-0184 item 3: the apply of a pad that changes a state of the window only, or null.
    private Action<double[]>? _padLocal;

    // ADR-0184 items 2 and 3: the text of a segment of the section: the name of the plane, and the offset with its unit.
    private string SegmentText(string label) => (label, _section) switch
    {
        (SectionPlaneLabel, { } shown) => shown.Plane.Name,
        (SectionOffsetLabel, { } shown) => $"{SketchText.Number(shown.Offset)} {_unit}",
        _ => RingText(label),
    };

    // ADR-0184 item 1: the segments of the section that have the colour of a tool in use: "Section" while a section lasts, and
    // "Flip" while the side is flipped.
    private bool SectionInUse(string label)
        => _partRing && _section is { } shown && (label == SectionLabel || (label == SectionFlipLabel && shown.Flip));

    // ADR-0184 item 6: Escape at the level of the document ends the section: with no selected body and no active sketch. With a
    // selection Escape goes up to the document first, so a second Escape ends the section.
    private void WatchSectionKeys(IReadOnlyList<InputSample> samples)
    {
        if (_section is not null && _bodyKey.Length == 0 && !_sketchActive
            && samples.Any(sample => sample is KeySample { Down: true, Key: InputKey.Escape }))
            _section = null;
    }

    // ADR-0184 item 6: the section ends with no live body, and on the home screen.
    private void FollowSection(IReadOnlyList<LiveBody> bodies)
    {
        if (bodies.Count == 0 || _home is not null)
            _section = null;
    }

    // ADR-0184 item 1: the icons of the section: a box with a cut line and its hatch for "Section", a plane in perspective for
    // "Plane", the mark of a distance for "Offset", and two opposite arrows for "Flip".
    private static bool SectionIcon(ImDrawListPtr list, string label, Vector2 c, uint ink, float h, float w)
    {
        switch (label)
        {
            case SectionLabel:
                list.AddRect(c - new Vector2(h, h * 0.8f), c + new Vector2(h, h * 0.8f), ink, 1f, ImDrawFlags.None, w);
                list.AddLine(c + new Vector2(0f, -h * 0.8f), c + new Vector2(0f, h * 0.8f), ink, w);
                foreach (var y in new[] { -0.4f, 0.1f, 0.6f })
                    list.AddLine(c + new Vector2(-h, h * y), c + new Vector2(-h * 0.4f, h * (y - 0.6f)), ink, w * 0.75f);
                return true;
            case SectionPlaneLabel:
                list.AddQuad(c + new Vector2(-h, h * 0.6f), c + new Vector2(-h * 0.4f, -h * 0.6f), c + new Vector2(h, -h * 0.6f), c + new Vector2(h * 0.4f, h * 0.6f), ink, w);
                return true;
            case SectionOffsetLabel:
                list.AddLine(c + new Vector2(-h, -h), c + new Vector2(-h, h), ink, w);
                list.AddLine(c + new Vector2(-h, 0f), c + new Vector2(h, 0f), ink, w);
                list.AddLine(c + new Vector2(h, 0f), c + new Vector2(h * 0.55f, -h * 0.4f), ink, w);
                list.AddLine(c + new Vector2(h, 0f), c + new Vector2(h * 0.55f, h * 0.4f), ink, w);
                return true;
            case SectionFlipLabel:
                list.AddLine(c + new Vector2(-h, -h * 0.4f), c + new Vector2(h, -h * 0.4f), ink, w);
                list.AddLine(c + new Vector2(h, -h * 0.4f), c + new Vector2(h * 0.55f, -h * 0.8f), ink, w);
                list.AddLine(c + new Vector2(h, h * 0.4f), c + new Vector2(-h, h * 0.4f), ink, w);
                list.AddLine(c + new Vector2(-h, h * 0.4f), c + new Vector2(-h * 0.55f, h * 0.8f), ink, w);
                return true;
            default:
                return false;
        }
    }
}
