using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0080 (the redesign, slice b1, TASK-0248): the fixed controls of the "mix" direction of the designer brief. Undo
// and Redo are two round anchors at the bottom left of the display. Each window is a rectangle of the panels, so a tap
// on it does not reach the scene.
// ADR-0093 (the redesign, slice j, TASK-0304): the title of the document is a top bar at the upper left with no ground,
// and the status line is at the lower right, in the mono font, as in the render of the brief.
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

    // The top of the row of the anchors in this frame, which the portrait ring stays above (V136). ADR-0093 (TASK-0304): the
    // top bar is a fixed window too, so the ring reads this field and not the lowest top of the fixed windows.
    private float _anchorsTop = float.MaxValue;

    // ADR-0093 item 1: the item of the top bar, and the height that it takes above the panel.
    public const string TitleItem = "Title";
    internal const float TopBarHeight = 32f;

    // ADR-0102 item 7 (TASK-0316): the item of the folder of the file in the top bar.
    public const string TitleFolderItem = "Title:folder";

    // ADR-0093 item 1: the top bar: the program, then the file or "unsaved", then the unit, with no ground. A long name is
    // cut to the width of the rows, so the unit stays.
    // Finding V149 of the codebase review of 2026-11-08 (c) (TASK-0311): and to the room left of the rows of a selected body,
    // 12 pixels from them, so on a narrow window the top bar does not cover their title; at least 48 pixels.
    // ADR-0099 item 5 (the redesign, slice p, TASK-0313): the rows are at the left now, so the name ends 12 pixels left of the
    // command line at the upper right.
    // ADR-0102 item 7 (phase P9g, TASK-0316): the folder of the file follows its name in the quiet colour, cut at its start so
    // that the last folders stay, where the room left of the command line holds at least 48 pixels of it.
    private void TopBar(Vector2 displaySize, DocumentLine? document)
    {
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X, 16f + _inset.Y), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("##TopBar", FixedFlags))
        {
            var colours = ImGui.GetStyle().Colors;
            var parts = TitleParts(document);
            var commandLeft = displaySize.X - 16f - _inset.Z - CommandWidth;
            var room = parts.Count == 3
                ? commandLeft - 12f - (16f + _inset.X) - ImGui.CalcTextSize(parts[0]).X - ImGui.CalcTextSize(parts[2]).X - 24f
                : RowsWidth;
            var nameWidth = Math.Clamp(room, 48f, RowsWidth);
            var folderRoom = parts.Count == 3 && document?.Folder is not null
                ? room - ImGui.CalcTextSize(Fit(parts[1], nameWidth)).X - 12f
                : 0f;
            ImGui.TextUnformatted(parts[0]);
            for (var i = 1; i < parts.Count; i++)
            {
                ImGui.SameLine(0f, 12f);
                ImGui.PushStyleColor(ImGuiCol.Text, colours[(int)(i == parts.Count - 1 ? ImGuiCol.TextDisabled : ImGuiCol.Text)]);
                ImGui.TextUnformatted(i == 1 && parts.Count == 3 ? Fit(parts[i], nameWidth) : parts[i]);
                ImGui.PopStyleColor();
                if (i == 1 && folderRoom >= 48f && document?.Folder is { } folder)
                {
                    ImGui.SameLine(0f, 12f);
                    ImGui.TextDisabled(FitStart(folder, folderRoom));
                    _items[TitleFolderItem] = new Vector4(ImGui.GetItemRectMin().X, ImGui.GetItemRectMin().Y, ImGui.GetItemRectMax().X, ImGui.GetItemRectMax().Y);
                }
            }

            AddWindowRectangle();
            _items[TitleItem] = _fixed[^1];
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    // ADR-0093 item 3: the text of the status line: the count of the live bodies, their state, and the address of the surface.
    internal string StatusText(IReadOnlyList<LiveBody> bodies, bool address = true)
    {
        var failed = bodies.Count(body => body.Fault is not null);
        var count = bodies.Count == 1 ? "1 body" : $"{bodies.Count} bodies";
        var state = failed == 0 ? "all correct" : $"{failed} failed";
        return !address || _address.Length == 0 ? $"{count} · {state}" : $"{count} · {state} · {_address}";
    }

    // Finding V146 of the codebase review of 2026-11-08 (c) (TASK-0310): the text of the status line that fits in this room
    // in the mono font: the whole line, else the line with no address, else none. Until TASK-0310 the line grew to the
    // left over Frame and Home on a phone, and took each tap on them.
    internal string? StatusFit(IReadOnlyList<LiveBody> bodies, float room)
    {
        ImGui.PushFont(_monoFont);
        try
        {
            foreach (var text in new[] { StatusText(bodies), StatusText(bodies, address: false) })
            {
                if (ImGui.CalcTextSize(text).X <= room)
                    return text;
            }

            return null;
        }
        finally
        {
            ImGui.PopFont();
        }
    }

    private void FixedControls(Vector2 displaySize, IReadOnlyList<LiveBody> bodies, DocumentLine? document)
    {
        TopBar(displaySize, document);

        // Undo and Redo at the bottom left (ADR-0031 item 11, amended by ADR-0080). Finding V118 of the codebase review of
        // 2026-11-06 (e) (TASK-0252): inside the safe area of the display, so that the navigation bar of the phone does not
        // cover them.
        ImGui.SetNextWindowPos(new Vector2(16f + _inset.X, displaySize.Y - 16f - _inset.W - AnchorSize), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        var anchorsRight = 16f + _inset.X;
        if (ImGui.Begin("##Anchors", FixedFlags))
        {
            if (Anchor(UndoLabel, back: true))
                _actions.Add(new UndoAction());
            ImGui.SameLine();
            if (Anchor(RedoLabel, back: false))
                _actions.Add(new RedoAction());
            // ADR-0083 (TASK-0259): "Frame" and "Home" of the demonstration, on the row of the anchors.
            ViewButtons();
            AddWindowRectangle();
            _anchorsTop = _fixed[^1].Y;
            anchorsRight = _fixed[^1].Z;
        }

        ImGui.End();

        // ADR-0093 item 3: the status line at the lower right, on the row of the anchors, in the mono font. Until TASK-0304 it
        // held the address only, right of Redo. V146 (TASK-0310): it starts at least 12 pixels right of the anchors, with the
        // text that fits there, or it does not show. Its left edge comes from the width of that text, so the first frame of a
        // new text is in its place too; a pivot at the right edge used the width of the last frame.
        var right = displaySize.X - 16f - _inset.Z;
        if (StatusFit(bodies, right - (anchorsRight + 12f)) is { } status)
        {
            ImGui.PushFont(_monoFont);
            var width = ImGui.CalcTextSize(status).X;
            ImGui.PopFont();
            ImGui.SetNextWindowPos(new Vector2(right - width, displaySize.Y - 16f - _inset.W - (AnchorSize / 2f)), ImGuiCond.Always, new Vector2(0f, 0.5f));
            if (ImGui.Begin("##Status", FixedFlags))
            {
                // The text colour of the panels: the dim colour gives 2.9:1 on the clear, below the 3:1 of large text.
                ImGui.PushFont(_monoFont);
                ImGui.TextUnformatted(status);
                ImGui.PopFont();
                Item(StatusLabel);
                AddWindowRectangle();
            }

            ImGui.End();
        }

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
