using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0078 item 1 (the redesign, slice a, TASK-0246): the theme "Proposed (harmonized)" of the design system of the
// owner (tokens.json, theme "proposed"): cool structure, warm attention, and a casing for the marks of the viewport. The
// theme changes colours only; each size and each place of a control stays (no change of layout).
internal static class Theme
{
    // The colours of the panels that the theme changes from the dark style of Dear ImGui, as the tokens give them.
    internal static readonly (ImGuiCol Slot, uint Rgba)[] Chrome =
    [
        (ImGuiCol.TextDisabled, 0x8A939FFF),   // chrome-text-disabled
        (ImGuiCol.WindowBg, 0x101419F0),       // chrome-window, alpha 0.94
        (ImGuiCol.PopupBg, 0x171B20F0),        // chrome-popup
        (ImGuiCol.Border, 0x6F7C8CFF),         // chrome-border, solid
        (ImGuiCol.TitleBg, 0x0A0E12FF),        // chrome-title
        (ImGuiCol.ScrollbarGrab, 0x464E58FF),  // chrome-scrollbar-grab
    ];

    // viewport-clear: a cool neutral at OKLCH L 0.40, hue 255, as bytes, so that a test compares the bytes of a picture.
    internal static readonly (byte R, byte G, byte B) Clear = (0x3F, 0x49, 0x55);

    // The marks of the viewport, as ImGui packs a colour (alpha, blue, green, red).
    internal const uint OtherSketch = 0xFFB8ACA3;   // sketch-other #a3acb8
    internal const uint Reference = 0xFF5690B9;     // sketch-reference #b99056
    internal const uint Snap = 0xFFD56CED;          // sketch-snap #ed6cd5
    internal const uint Casing = 0xFF16100B;        // sketch-casing #0b1016

    // stroke-casing: the width of the casing on each side of a mark.
    internal const float CasingWidth = 2f;

    // Sets the colours of the theme on the style of a context.
    public static void Apply(ImGuiStylePtr style)
    {
        foreach (var (slot, rgba) in Chrome)
            style.Colors[(int)slot] = Vector(rgba);
    }

    internal static Vector4 Vector(uint rgba)
        => new(((rgba >> 24) & 0xFF) / 255f, ((rgba >> 16) & 0xFF) / 255f, ((rgba >> 8) & 0xFF) / 255f, (rgba & 0xFF) / 255f);
}
