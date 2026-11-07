using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0080 (the redesign, slice b1, TASK-0248): the fixed controls of the "mix" direction of the designer brief. Undo
// and Redo are two round anchors at the bottom left of the display, and the status line, with the address of the
// surface, is on their row. The title of the document is the title bar of the panel (see Panel). Each window is
// a rectangle of the panels, so a tap on it does not reach the scene.
internal sealed unsafe partial class DesktopUi
{
    // size-anchor of the design system: the round fixed controls, 48 pixels, at least the 44 of ADR-0027 item 6.
    public const float AnchorSize = 48f;

    // The space that the anchors take under the panel: their size and the margin above them.
    internal const float AnchorRow = AnchorSize + 16f;

    private const ImGuiWindowFlags FixedFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground
        | ImGuiWindowFlags.NoFocusOnAppearing;

    public const string StatusLabel = "Status";

    private void FixedControls(Vector2 displaySize)
    {
        // Undo and Redo at the bottom left (ADR-0031 item 11, amended by ADR-0080). Finding V118 of the codebase review of
        // 2026-11-06 (e) (TASK-0252): inside the safe area of the display, so that the navigation bar of the phone does not
        // cover them.
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X, displaySize.Y - 16f - _inset.W - AnchorSize), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("##Anchors", FixedFlags))
        {
            if (Anchor(UndoLabel, back: true))
                _actions.Add(new UndoAction());
            ImGui.SameLine();
            if (Anchor(RedoLabel, back: false))
                _actions.Add(new RedoAction());
            AddWindowRectangle();
        }

        ImGui.End();

        // The status line on the row of the anchors, right of Redo: the address of the surface of the session. The brief puts it
        // at the bottom right, where the window of a sketch lies today; the row of the anchors holds no panel.
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X + (2 * AnchorSize) + 28f, displaySize.Y - 16f - _inset.W - (AnchorSize / 2f)), ImGuiCond.Always, new Vector2(0f, 0.5f));
        if (ImGui.Begin("##Status", FixedFlags))
        {
            // The text colour of the panels: the dim colour gives 2.9:1 on the clear, below the 3:1 of large text.
            ImGui.TextUnformatted(_address);
            Item(StatusLabel);
            AddWindowRectangle();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void AddWindowRectangle()
    {
        var (position, size) = (ImGui.GetWindowPos(), ImGui.GetWindowSize());
        _fixed.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
    }

    // A round button with a bent arrow: to the left for Undo, to the right for Redo. The bitmap font has no arrow, so the
    // draw list draws it.
    private bool Anchor(string label, bool back)
    {
        var pressed = ImGui.InvisibleButton(label, new Vector2(AnchorSize));
        Item(label);
        var (min, max) = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        var centre = (min + max) / 2f;
        var colours = ImGui.GetStyle().Colors;
        var ground = ImGui.IsItemActive() ? colours[(int)ImGuiCol.ButtonActive] : ImGui.IsItemHovered() ? colours[(int)ImGuiCol.ButtonHovered] : colours[(int)ImGuiCol.Button];
        var list = ImGui.GetWindowDrawList();
        list.AddCircleFilled(centre, AnchorSize / 2f, ImGui.GetColorU32(ground));
        list.AddCircle(centre, AnchorSize / 2f, ImGui.GetColorU32(colours[(int)ImGuiCol.Border]), 0, 1.5f);

        // The arrow: an arc over the top, and a head at its lower end.
        var ink = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
        var side = back ? -1f : 1f;
        const float radius = 10f;
        list.PathArcTo(centre + new Vector2(0f, 3f), radius, MathF.PI * 1.1f, MathF.PI * 1.9f);
        list.PathStroke(ink, ImDrawFlags.None, 2.5f);
        var tip = centre + new Vector2(side * radius, 3f + 1f);
        list.AddTriangleFilled(tip + new Vector2(0f, 6f), tip + new Vector2(-side * 6f, -2f), tip + new Vector2(side * 6f, -2f), ink);
        return pressed;
    }
}
