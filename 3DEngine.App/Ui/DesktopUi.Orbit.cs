using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0095 (the redesign, slice l, TASK-0306): the look of the orbit ring of the render "mix". A landscape ring has a faint
// guide circle at its inner and outer edges; each segment has a light ground, an outline, a drawn icon above its label and
// the short label of the render; and a caption "TOOLS FOR" and the name of the selection is to the right of a body ring.
// The places of the segments and the taps do not change (ADR-0081 and its amendments).
internal sealed unsafe partial class DesktopUi
{
    // The item of the icon of a segment, and of the caption of the ring.
    public static string RingIconItem(string label) => $"RingIcon:{label}";

    public const string RingCaptionItem = "Ring:caption";

    // The half size of an icon, and the space between the icon and the label under it.
    private const float IconHalf = 9f;
    private const float IconGap = 4f;

    // The short label of a segment, as the render writes it; the key of the segment stays its label.
    internal static string RingText(string label) => label switch
    {
        SetDistanceLabel => "Distance",
        SketchOnFaceButtonLabel => "Sketch",
        _ => label,
    };

    // The guide of a landscape ring: a faint circle at its inner edge and one at its outer edge.
    private static void RingGuide(ImDrawListPtr list, RingLayout ring)
    {
        var faint = ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Border] with { W = 0.35f });
        list.AddCircle(ring.Centre, ring.Inner, faint, 128, 1f);
        list.AddCircle(ring.Centre, ring.Outer, faint, 128, 1f);
    }

    // The caption of a body ring: "TOOLS FOR" and the name, in capitals with a space after each letter, to the right of the
    // middle of the band, in the dim colour; none when it does not fit in the display.
    private void RingCaption(ImDrawListPtr list, RingLayout ring, string name, Vector2 displaySize)
    {
        var caption = string.Join(" ", $"TOOLS FOR {name}".ToUpperInvariant().ToCharArray()).Replace("   ", "  ");
        var size = ImGui.CalcTextSize(caption);
        var place = new Vector2(ring.Centre.X + ring.Outer + 24f, ring.Centre.Y - (size.Y / 2f));
        if (place.X + size.X > displaySize.X - 16f - _inset.Z)
            return;
        list.AddText(place, ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]), caption);
        _items[RingCaptionItem] = new Vector4(place.X, place.Y, place.X + size.X, place.Y + size.Y);
    }

    // The icon of a segment, drawn with the draw list at its centre: none for a label with no icon.
    private static bool RingIcon(ImDrawListPtr list, string label, Vector2 c, uint ink)
    {
        const float h = IconHalf;
        const float w = 1.6f;
        switch (label)
        {
            case MoveLabel:
                list.AddLine(c - new Vector2(h, 0f), c + new Vector2(h, 0f), ink, w);
                list.AddLine(c - new Vector2(0f, h), c + new Vector2(0f, h), ink, w);
                foreach (var d in new[] { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY })
                {
                    var tip = c + (d * h);
                    var side = new Vector2(-d.Y, d.X) * 3f;
                    list.AddLine(tip, tip - (d * 3f) + side, ink, w);
                    list.AddLine(tip, tip - (d * 3f) - side, ink, w);
                }

                return true;
            case SetDistanceLabel:
            case "Distance":
                list.AddLine(c - new Vector2(0f, h), c + new Vector2(0f, h), ink, w);
                list.AddLine(c + new Vector2(-h * 0.6f, -h), c + new Vector2(h * 0.6f, -h), ink, w);
                list.AddLine(c + new Vector2(-h * 0.6f, h), c + new Vector2(h * 0.6f, h), ink, w);
                return true;
            case HideLabel:
            case ShowLabel:
                list.PathArcTo(c + new Vector2(0f, h * 1.1f), h * 1.5f, -MathF.PI * 0.8f, -MathF.PI * 0.2f);
                list.PathArcTo(c - new Vector2(0f, h * 1.1f), h * 1.5f, MathF.PI * 0.2f, MathF.PI * 0.8f);
                list.PathStroke(ink, ImDrawFlags.Closed, w);
                list.AddCircle(c, h * 0.35f, ink, 12, w);
                if (label == HideLabel)
                    list.AddLine(c + new Vector2(-h, h), c + new Vector2(h, -h), ink, w);
                return true;
            case SketchOnFaceButtonLabel:
                list.AddLine(c + new Vector2(-h, h), c + new Vector2(h * 0.6f, -h * 0.6f), ink, w + 1f);
                list.AddLine(c + new Vector2(-h, h), c + new Vector2(-h * 0.4f, h), ink, w);
                return true;
            case LineToolLabel:
                list.AddLine(c + new Vector2(-h, h), c + new Vector2(h, -h), ink, w);
                return true;
            case SelectToolLabel:
                list.AddTriangle(c + new Vector2(-h * 0.6f, -h), c + new Vector2(-h * 0.6f, h * 0.7f), c + new Vector2(h * 0.6f, h * 0.2f), ink, w);
                return true;
            case CircleToolLabel:
                list.AddCircle(c, h, ink, 24, w);
                return true;
            case ArcToolLabel:
                list.PathArcTo(c + new Vector2(0f, h * 0.6f), h, -MathF.PI * 0.85f, -MathF.PI * 0.15f);
                list.PathStroke(ink, ImDrawFlags.None, w);
                return true;
            case ConstrainLabel:
                list.AddLine(c + new Vector2(-h, -h * 0.35f), c + new Vector2(h, -h * 0.35f), ink, w);
                list.AddLine(c + new Vector2(-h, h * 0.35f), c + new Vector2(h, h * 0.35f), ink, w);
                return true;
            case FinishLabel:
                list.AddLine(c + new Vector2(-h, 0f), c + new Vector2(-h * 0.25f, h * 0.7f), ink, w + 0.4f);
                list.AddLine(c + new Vector2(-h * 0.25f, h * 0.7f), c + new Vector2(h, -h * 0.7f), ink, w + 0.4f);
                return true;
            case BackLabel:
                list.AddLine(c + new Vector2(h, 0f), c - new Vector2(h, 0f), ink, w);
                list.AddLine(c - new Vector2(h, 0f), c + new Vector2(-h * 0.3f, -h * 0.6f), ink, w);
                list.AddLine(c - new Vector2(h, 0f), c + new Vector2(-h * 0.3f, h * 0.6f), ink, w);
                return true;
            default:
                return false;
        }
    }
}
