using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0175 (phase R31b, TASK-0420): the density of the rows of the structure view. "Compact" gives rows of 30 pixels with
// the text of a list, for a mouse or a pen; "For touch" gives rows of 44 pixels, the target of a finger of ADR-0027 item 6.
// The device gives the first density, and the net of the views changes it. Only the rows of the structure view change: the
// ring, the anchors, the pad, the filter, "Show all" and each other control keep 44 pixels.
internal sealed unsafe partial class DesktopUi
{
    // ADR-0175 item 1: the two densities.
    internal enum RowDensity
    {
        Compact,
        ForTouch,
    }

    // ADR-0175 item 2: the words of the two cells of the net of the views.
    public const string CompactLabel = "Compact";
    public const string ForTouchLabel = "For touch";

    // ADR-0175 item 1: the height of a row of the structure view in the density "Compact", and the size of the text of a list.
    public const float CompactRowHeight = 30f;
    public const float ListSize = 20f;

    // ADR-0175 item 2: the columns of the two cells on the row of "Top", at its left and at its right.
    internal const int CompactColumn = 0;
    internal const int ForTouchColumn = 2;
    internal const int DensityRow = 0;

    // ADR-0175 item 1: a phone starts "For touch", and each other device starts "Compact". The choice depends on the device
    // and on the net of the views, not on the last input, so the rows do not move under a finger.
    internal static RowDensity DeviceDensity => OperatingSystem.IsAndroid() ? RowDensity.ForTouch : RowDensity.Compact;

    private RowDensity _density = DeviceDensity;

    // ADR-0175 item 2: the density of this frame; a test sets it as a tap on its cell does.
    internal RowDensity Density
    {
        get => _density;
        set => _density = value;
    }

    // ADR-0175 item 1: the height of a row of the structure view in this density.
    internal float StructureRowHeight => _density == RowDensity.Compact ? CompactRowHeight : RowHeight;

    // ADR-0175 item 2: "Compact" at the left of "Top" and "For touch" at its right. The cell of the density in use has the
    // colour of a tool in use. A tap changes the density and does not close the net, so the person sees the rows change.
    // The text of a cell that is too wide gets smaller, as for the switches of the dimensions.
    private void DensityCells(Vector2 cell, Vector4 accent)
    {
        foreach (var (column, label, density) in new[] { (CompactColumn, CompactLabel, RowDensity.Compact), (ForTouchColumn, ForTouchLabel, RowDensity.ForTouch) })
        {
            ImGui.SetCursorPos(new Vector2(column * (cell.X + ViewGap), DensityRow * (cell.Y + ViewGap)));
            var on = _density == density;
            if (on)
                ImGui.PushStyleColor(ImGuiCol.Button, accent with { W = 0.3f });
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(PadKeyMargin, ImGui.GetStyle().FramePadding.Y));
            ImGui.SetWindowFontScale(Math.Clamp((cell.X - (2f * PadKeyMargin)) / Math.Max(ImGui.CalcTextSize(label).X, 1f), ViewSwitchLeastScale, 1f));
            var pressed = ImGui.Button($"{label}##View", cell);
            ImGui.SetWindowFontScale(1f);
            ImGui.PopStyleVar();
            if (pressed)
                _density = density;
            if (on)
                ImGui.PopStyleColor();
            Item(label);
            if (_items.TryGetValue(label, out var box))
                ImGui.GetWindowDrawList().AddRect(new Vector2(box.X + 1f, box.Y + 1f), new Vector2(box.Z - 1f, box.W - 1f), ImGui.GetColorU32(on ? ImGuiCol.ButtonActive : ImGuiCol.Border), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, Theme.OutlineWidth);
        }
    }

    // ADR-0175 item 1: the font of the text of a list, Instrument Sans at ListSize.
    private readonly ImFontPtr _listFont;

    // ADR-0175 items 1 and 4: the size of the font of a list, the width of a text in it, and the size of the text of the body
    // rows of the last frame, for a test.
    internal float ListFontSize => _listFont.FontSize;

    internal float ListTextWidth(string text) => _listFont.CalcTextSizeA(ListSize, float.MaxValue, 0f, text).X;

    private float _structureRowFont;

    internal float StructureRowFontSize => _structureRowFont;
}
