using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0078 item 1 (the redesign, slice a, TASK-0246): the theme "Proposed (harmonized)" of the design system of the
// owner (tokens.json, theme "proposed"): cool structure, warm attention, and a casing for the marks of the viewport. The
// theme changes colours only; each size and each place of a control stays (no change of layout).
// ADR-0108 (phase R18a, TASK-0332): the look of the render "mix": an almost black ground, and thin outlined controls with
// no fill; a hover and a press give a light fill of the accent. Until TASK-0332 the ground was a cool grey and each
// control had a filled blue ground.
internal static class Theme
{
    // The colours of the panels that the theme changes from the dark style of Dear ImGui, as the tokens give them.
    internal static readonly (ImGuiCol Slot, uint Rgba)[] Chrome =
    [
        (ImGuiCol.TextDisabled, 0x8A939FFF),   // chrome-text-disabled
        (ImGuiCol.WindowBg, 0x0B1219F0),       // chrome-window, alpha 0.94, a step above the ground of ADR-0108
        (ImGuiCol.PopupBg, 0x10171EF0),        // chrome-popup
        (ImGuiCol.Border, 0x4A7480FF),         // ADR-0108 item 2: the outline of a control, 3.8:1 on the ground
        (ImGuiCol.TitleBg, 0x05090DFF),        // chrome-title
        (ImGuiCol.ScrollbarGrab, 0x464E58FF),  // chrome-scrollbar-grab
        (ImGuiCol.Button, 0x30EAFF00),         // ADR-0108 item 2: no fill at rest
        (ImGuiCol.ButtonHovered, 0x30EAFF29),  // the accent at alpha 0.16
        (ImGuiCol.ButtonActive, 0x30EAFF52),   // the accent at alpha 0.32
        (ImGuiCol.FrameBg, 0x30EAFF00),        // a field has no fill at rest
        (ImGuiCol.FrameBgHovered, 0x30EAFF1F), // the accent at alpha 0.12
        (ImGuiCol.FrameBgActive, 0x30EAFF33),  // the accent at alpha 0.20
        (ImGuiCol.CheckMark, 0x30EAFFFF),      // the accent
    ];

    // ADR-0108 item 1: the clear of the scene, the almost black ground of the render, as bytes, so that a test compares the
    // bytes of a picture. Until TASK-0332 it was viewport-clear #3f4955 of ADR-0078.
    internal static readonly (byte R, byte G, byte B) Clear = (0x07, 0x0D, 0x14);

    // The marks of the viewport, as ImGui packs a colour (alpha, blue, green, red).
    internal const uint OtherSketch = 0xFFB8ACA3;   // sketch-other #a3acb8
    internal const uint Reference = 0xFF5690B9;     // sketch-reference #b99056
    internal const uint Snap = 0xFFD56CED;          // sketch-snap #ed6cd5
    internal const uint Casing = 0xFF16100B;        // sketch-casing #0b1016

    // ADR-0108 items 2 and 5, as ImGui packs a colour: the accent of a control (an icon, the mark of the tool in use), and
    // the colour of the state "correct".
    internal const uint Accent = 0xFFFFEA30;        // #30eaff
    internal const uint Correct = 0xFFBFF48C;       // #8cf4bf

    // ADR-0108 item 2: the width of the outline of a control, and of the mark of the tool in use.
    internal const float OutlineWidth = 1f;
    internal const float InUseWidth = 2.5f;

    // stroke-casing: the width of the casing on each side of a mark.
    internal const float CasingWidth = 2f;

    // Sets the colours of the theme on the style of a context.
    public static void Apply(ImGuiStylePtr style)
    {
        foreach (var (slot, rgba) in Chrome)
            style.Colors[(int)slot] = Vector(rgba);
        // ADR-0108 item 2: each field and each button of a window has its outline.
        style.FrameBorderSize = OutlineWidth;
    }

    // ADR-0108 item 2: the ground under a text that lies over the scene, as ImGui packs a colour, so the text reads over a
    // body.
    internal static uint Plate(float alpha) => ((uint)(alpha * 255f) << 24) | ((uint)Clear.B << 16) | ((uint)Clear.G << 8) | Clear.R;

    internal static Vector4 Vector(uint rgba)
        => new(((rgba >> 24) & 0xFF) / 255f, ((rgba >> 16) & 0xFF) / 255f, ((rgba >> 8) & 0xFF) / 255f, (rgba & 0xFF) / 255f);
}
