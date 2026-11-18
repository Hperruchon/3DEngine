using System.Numerics;
using System.Globalization;
using ImGuiNET;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Core.Sketching;

namespace ThreeDEngine.Desktop.Ui;

// Phase R15 (TASK-0234): the numeric pad (ADR-0032), a part of DesktopUi. Until TASK-0234 it was in DesktopUi.cs; no
// line changed in the split.
internal sealed unsafe partial class DesktopUi
{
    // The open pad, or null.
    public NumericPad? Pad => _pad;

    // The key of the rectangle of a control of the pad, for ItemRectangle.
    public static string PadItem(string label) => $"{PadName} {label}";

    // The host gives the result of the command of an apply: null closes the pad, and a
    // rejection stays in the pad (ADR-0032 item 8).
    public void EndPad(string? rejection)
    {
        _padApplied = false;
        if (rejection is null)
            ClosePad();
        else
            _pad?.Fail(rejection);
    }

    // ADR-0034 item 7: the pad of the next point of a chain, an offset from the last point while a
    // chain is open, else a position. The host opens it again after each apply, so a person types
    // each point of a chain in one pad.
    public void OpenNextPointPad(bool chainOpen)
    {
        var (a, b) = chainOpen ? ("dx", "dy") : ("x", "y");
        // The key comes from the state that names the fields, not from the last frame: the host
        // opens the pad again in the frame of an apply, before the panel sees the new chain.
        OpenPad(new NumericPad(NextPointLabel, _unit, new(a, "0"), new(b, "0")), values => new NextPointAction(values[0], values[1]), bySketch: true,
            scope: PadScope.Chain, key: ChainKey(_activeKey, SketchToolKind.Line, chainOpen));
    }

    // Finding V119 of the codebase review of 2026-11-07 (TASK-0264): a new pad ends the pending state of a chip; the pad of
    // a chip and the pad of the distance set it again after the open (ADR-0082 items 1 and 4).
    private void OpenPad(NumericPad pad, Func<double[], PanelAction> apply, bool bySketch = false, PadScope scope = PadScope.None, string? key = null)
    {
        (_pad, _padApply, _padBySketch, _padApplied) = (pad, apply, bySketch, false);
        // ADR-0167 item 5 (TASK-0407): one pad at a time.
        _sheetPad = null;
        (_padScope, _padKey) = (scope, key ?? Key(scope));
        _pendingValue = null;
    }

    private void ClosePad() => (_pad, _padApply, _padScope, _padKey, _padApplied) = (null, null, PadScope.None, null, false);

    // ADR-0049 item 8: the pad of the distance of the selected extrude, which starts with the
    // distance in effect. A change of the selection closes it, as for Move.
    // ADR-0082 item 4 (TASK-0254): the chip of the distance is pending while this pad is open, from the chip, the ring or
    // the panel, because each of them changes the same value.
    public void OpenDistancePad(Guid body, double distance) => OpenDistancePad(body, SketchText.Number(distance));

    // ADR-0128 item 2 (phase R18h, TASK-0357): the same pad with a start text, which a value typed in the command line gives.
    private void OpenDistancePad(Guid body, string start)
    {
        OpenPad(new NumericPad(SetDistanceLabel, _unit, new PadField("distance", start)), values => new SetDistanceAction(body, values[0]), scope: PadScope.Body);
        _pendingValue = EdgeValues.DistanceKey(body);
    }

    // ADR-0032 item 5: the window of the pad. A row selects its field; the line under the fields
    // gives the value of the active field, or its error, and the message of the last apply.
    // ADR-0172 item 2 (TASK-0418): the pad is a fixed part of the layout, as the panel. It has no title bar, the person does
    // not move it, and the frame sets its place and its size in each frame. Its keys fill its width. It takes the place of
    // the panel and of the rows, which do not show while it is open, under the top bar and above the anchors; a pad of a
    // sketch opens beside the rows of the sketch when it has room there, else under them. A pad with no room for its five
    // lines of keys is compact (PlacePad). Until TASK-0418 it opened at x 492 with keys of 108 pixels, so on a phone three
    // columns of keys were outside the display, and it covered the top bar, the command line and the anchors.
    private void PadWindow(NumericPad pad, Vector2 displaySize)
    {
        var portrait = Portrait(displaySize);
        var left = 16f + _inset.X;
        var right = displaySize.X - 16f - _inset.Z;
        var top = PadTop(displaySize);
        var bottom = displaySize.Y - 16f - (_padLow ? 0f : AnchorRow) - _inset.W;
        if (_padBySketch && _sketchWindow is { } sketch)
        {
            if (right - (sketch.Z + 16f) >= PadLeast)
                left = sketch.Z + 16f;
            else
                top = sketch.W + 8f;
        }

        var width = right - left;
        if (!portrait && !_padCompact)
        {
            // The pad beside the command line ends 12 pixels left of it, as the panel does.
            var beside = Math.Min(PadWidth, CommandLeft(displaySize) - 12f - left);
            if (beside >= PadLeast)
                width = beside;
            else
                top = Math.Max(top, CommandBottom);
        }

        var rows = PadLines(portrait);
        var height = Math.Max(Math.Min(PadNeed(pad, rows.Length, _padCompact), bottom - top), MinimumControlHeight);
        ImGui.SetNextWindowPos(new Vector2(left, top), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
        if (ImGui.Begin(PadName, PadFlags))
        {
            // A compact pad has its fields on one line, and its title on the line of the value.
            if (!_padCompact)
                ImGui.TextUnformatted(pad.Title);
            var fields = pad.Fields;
            var fieldWidth = _padCompact ? MathF.Floor((ImGui.GetContentRegionAvail().X - ((fields.Count - 1) * ImGui.GetStyle().ItemSpacing.X)) / fields.Count) : 0f;
            for (var index = 0; index < fields.Count; index++)
            {
                if (_padCompact && index > 0)
                    ImGui.SameLine();
                if (ImGui.Selectable($"{fields[index].Name} = {fields[index].Text}  {pad.Unit}##field{index}", pad.Active == index, ImGuiSelectableFlags.None, new Vector2(fieldWidth, MinimumControlHeight)))
                    pad.Select(index);
                Item(PadItem(fields[index].Name));
            }

            var preview = pad.Preview;
            var value = preview.IsValue ? $"= {preview.Value.ToString("G15", CultureInfo.InvariantCulture)} {pad.Unit}" : preview.Error!;
            ImGui.TextUnformatted(_padCompact ? $"{pad.Title}   {value}" : value);
            if (pad.Message is { } message)
                ImGui.TextUnformatted(message);
            ImGui.Separator();

            // The keys of each line fill the width, with a narrow inner margin. A text wider than its key is smaller, to
            // PadKeyLeastScale of the font at least, so "Delete" and "Cancel" fit a key on a phone.
            var avail = ImGui.GetContentRegionAvail().X;
            var gap = ImGui.GetStyle().ItemSpacing.X;
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(PadKeyMargin, ImGui.GetStyle().FramePadding.Y));
            foreach (var row in rows)
            {
                var key = MathF.Floor((avail - ((row.Length - 1) * gap)) / row.Length);
                for (var column = 0; column < row.Length; column++)
                {
                    if (column > 0)
                        ImGui.SameLine();
                    var text = ImGui.CalcTextSize(row[column]).X;
                    ImGui.SetWindowFontScale(Math.Clamp((key - (2f * PadKeyMargin)) / Math.Max(text, 1f), PadKeyLeastScale, 1f));
                    if (ImGui.Button(row[column], new Vector2(key, MinimumControlHeight)))
                        Press(pad, row[column]);
                    ImGui.SetWindowFontScale(1f);
                    Item(PadItem(row[column]));
                }
            }

            ImGui.PopStyleVar();
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();
    }

    // ADR-0172 item 2 (TASK-0418): the inner margin of a key at each side, the least scale of the text of a key, and the width
    // of the text of a key at that scale with its margin, for a test.
    internal const float PadKeyMargin = 2f;
    internal const float PadKeyLeastScale = 0.7f;

    internal float PadKeyNeed(string label)
    {
        ImGui.SetCurrentContext(_context);
        return (ImGui.CalcTextSize(label).X * PadKeyLeastScale) + (2f * PadKeyMargin);
    }

    // ADR-0172 item 2 (TASK-0418): the pad has no title bar and no move, and a long pad scrolls.
    private const ImGuiWindowFlags PadFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings;

    // ADR-0172 item 2: the least width of a pad beside the rows of a sketch or the command line, five keys of 52 pixels.
    internal const float PadLeast = (5f * 52f) + (4f * 12f) + (2f * 14f);

    // ADR-0172 item 2: the keys on three lines for a compact pad on a landscape display: two lines of PadRows on the first line,
    // the next two lines with no Delete on the second, and Delete with the actions on the third, as ADR-0032 item 5 groups them.
    public static readonly string[][] PadLinesOfTen =
    [
        [.. PadRows[0], .. PadRows[1]],
        [.. PadRows[2], .. PadRows[3][..^1]],
        [PadRows[3][^1], .. PadRows[4]],
    ];

    // ADR-0172 item 2: the lines of keys of the pad of this frame.
    private string[][] PadLines(bool portrait) => _padCompact && !portrait ? PadLinesOfTen : PadRows;

    // ADR-0172 item 2: the top of the pad: under the top bar, or under the command line for a compact pad on a landscape
    // display, which takes the full width.
    private float PadTop(Vector2 displaySize) => _padCompact && !Portrait(displaySize) ? Math.Max(PanelTop, CommandBottom) : PanelTop;

    // The bottom of the command line on a landscape display, and the gap under it.
    private float CommandBottom => 16f + _inset.Y + MinimumControlHeight + 8f;

    // ADR-0172 item 2: the height of the pad with a number of lines of keys: the padding, the title, a row of each field,
    // the line of the value, the line of a message, the separator, and the lines of keys, each with the gap of an item. A
    // compact pad has one line of fields and no line of its own for the title.
    private float PadNeed(NumericPad pad, int lines, bool compact = false)
    {
        var style = ImGui.GetStyle();
        var gap = style.ItemSpacing.Y;
        var text = ImGui.GetTextLineHeight() + gap;
        var fields = compact ? MinimumControlHeight + gap : text + (pad.Fields.Count * (MinimumControlHeight + gap));
        return (2f * style.WindowPadding.Y) + fields + text
            + (pad.Message is null ? 0f : text) + 1f + gap + (lines * (MinimumControlHeight + gap)) - gap;
    }

    // ADR-0172 item 2 (TASK-0418): a pad with no room for its title, its fields and five lines of keys above the anchors is
    // compact: its fields go on one line and its title on the line of the value, and on a landscape display it goes under
    // the command line at the full width with its keys on lines of ten. A compact pad that still has no room is low: the
    // anchors and the status line do not show while it is open, and it goes down to the bottom margin. Until TASK-0418 a pad
    // with three fields scrolled its keys out of view at 360 by 560, and the pad covered the anchors on a landscape phone.
    private bool _padCompact;
    private bool _padLow;

    // ADR-0172 item 2: the state of the pad of this frame, before the anchors read it.
    private void PlacePad(Vector2 displaySize)
    {
        (_padCompact, _padLow) = (false, false);
        if (_pad is not { } pad)
            return;
        var above = displaySize.Y - 16f - AnchorRow - _inset.W;
        _padCompact = PanelTop + PadNeed(pad, PadRows.Length) > above;
        if (_padCompact)
            _padLow = PadTop(displaySize) + PadNeed(pad, PadLines(Portrait(displaySize)).Length, true) > above;
    }

    // A button of the pad: an edit, an apply, a cancel, or the text of its label.
    private void Press(NumericPad pad, string label)
    {
        switch (label)
        {
            case "Delete": pad.Delete(); break;
            case "Clear": pad.Clear(); break;
            case "Next": pad.Next(); break;
            case "Apply": ApplyPad(pad); break;
            case "Cancel": ClosePad(); break;
            default: pad.Append(label); break;
        }
    }

    // ADR-0032 item 6: a key or a text of the frame while the pad is open.
    private void PadKey(NumericPad pad, InputSample sample)
    {
        switch (sample)
        {
            case TextSample text: pad.Append(text.Text); break;
            case KeySample { Down: true, Key: InputKey.Backspace }: Press(pad, "Delete"); break;
            case KeySample { Down: true, Key: InputKey.Tab }: Press(pad, "Next"); break;
            case KeySample { Down: true, Key: InputKey.Enter }: Press(pad, "Apply"); break;
            case KeySample { Down: true, Key: InputKey.Escape }: Press(pad, "Cancel"); break;
        }
    }

    // The numbers of an apply go to the host, and the pad stays open until EndPad. A second
    // apply before EndPad does nothing (V34): two presses of Enter in one frame gave two boxes.
    private void ApplyPad(NumericPad pad)
    {
        if (_padApplied)
            return;
        if (pad.Apply() is { } values && _padApply is { } apply)
        {
            _padAction = apply(values);
            _actions.Add(_padAction);
            _padApplied = true;
        }
    }
}
