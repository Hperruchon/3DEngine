using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0084 (the redesign, slice d, TASK-0267): the rows of the selected body, with no panel. The "mix" direction of the
// designer brief puts the data of the selected part in rows with no ground and no border: its name and its state, then
// each value that a command changes, with its label and its exact value. A tap on a value row opens the pad of that value,
// as its chip on the edge does (ADR-0082).
// ADR-0117 (phase R18d, TASK-0342): the rows as in the render: the name larger and bold, with its state on the same line
// when it has room; the row "Made from" with the sketch of the body, whose tap opens that sketch; each value with its label
// from its direction, the name of its constraint, and the value in the mono font.
internal sealed unsafe partial class DesktopUi
{
    // The width of the column of the rows, and the height of one row: the 44 pixels of a target of ADR-0027 item 6.
    public const float RowsWidth = 360f;
    public const float RowHeight = ValueSize;

    // The item of the first row: the name and the state of the body.
    public const string RowsTitleItem = "Rows:title";

    // ADR-0099 item 2 (the redesign, slice p, TASK-0313): the item of the name field that a tap on the first row opens.
    public const string RenameItem = "Rows:rename";

    // The colour of the word "failed" and its code (the brief: red always comes with the word "failed" and the code). The
    // theme of ADR-0078 has no colour of a fault, so this file holds it. Finding V133 (TASK-0271): until TASK-0271 this
    // comment said that it was a colour of the theme.
    internal const uint FailedColour = 0xFF5C5CE8;

    public static string RowItem(string key) => $"Row:{key}";

    // ADR-0117 item 2: the row of the sketch that made the body, its label, and its step of the history; null with none.
    public const string MadeFromItem = "Rows:madeFrom";
    public const string MadeFromLabel = "Made from";
    private (HistoryStep Step, Vector4 Rectangle)? _madeFrom;

    // ADR-0117 item 1: the height of the first row of this frame: one row, or two lines when the state has no room after the
    // name.
    private float _titleHeight = RowHeight;

    // ADR-0117 item 1: the gap between the name and the state on one line.
    internal const float TitleGap = 12f;

    // The value rows of this frame, and the rectangle of each row with the first one.
    private List<(EdgeValues.Value Value, Vector4 Rectangle)> _rows = [];
    private Vector4? _rowsArea;

    // ADR-0099 item 3: the lines under the values, each with the item of its line in the panel, and its rectangle.
    private List<(string Item, string Text, Vector4 Rectangle)> _rowLines = [];

    // ADR-0099 item 4: the row of the list of a cut, and the visible bodies that can cut the selected body.
    private Vector4? _cutRow;
    private List<LiveBody> _cutTools = [];

    // ADR-0131 item 3 (phase R18k, TASK-0360): the row of "Hidden too" while the dimensions show, and its item. A portrait
    // display has no command line, and the panel does not show with a selected body, so this row keeps the toggle reachable.
    public const string HiddenTooItem = "Row:hidden too";
    private Vector4? _hiddenTooRow;

    // ADR-0099 item 2: the body whose name field is open, or null; and the focus of the field in the next frame.
    private Guid? _renaming;
    private bool _focusRename;

    // ADR-0099 item 1: true when the rows of this body show, so that the panel does not. ADR-0131 item 1 (phase R18k,
    // TASK-0360): on a portrait display too, under the part.
    private bool RowsShow(Vector2 displaySize, LiveBody? body)
        => body is not null && !_sketchActive;

    // ADR-0131 item 1: true when the rows of this frame are under the part, on a portrait display.
    private bool _rowsUnder;

    // The rectangle of the rows of this frame, for a test; null with no rows.
    internal Vector4? RowsArea => _rowsArea;

    // True when the point is on the rows of this frame.
    private bool OverRows(double x, double y)
        => _rowsArea is { } area && x >= area.X && x < area.Z && y >= area.Y && y < area.W;

    private EdgeValues.Value? RowAt(Vector2 point)
        => _rows.FirstOrDefault(row => point.X >= row.Rectangle.X && point.X < row.Rectangle.Z && point.Y >= row.Rectangle.Y && point.Y < row.Rectangle.W).Value;

    // The state of the body after its kind: "correct", or "failed" and the code of its fault; and "hidden".
    internal static string RowState(LiveBody body)
        => $"{body.Kind} · {(body.Fault is { } fault ? $"failed {fault.Code}" : "correct")}{(body.Visible ? string.Empty : " · hidden")}";

    // Item 1: on a landscape display, while a body is selected and no sketch is active, the rows show. Until TASK-0360 a
    // portrait display kept the data in the panel (ADR-0131 item 1). The frame places the rows before the ring and the
    // chips, so that both read the rows of this frame.
    // ADR-0099 item 1 (the redesign, slice p, TASK-0313): the rows are at the upper left, in the place of the panel, which
    // does not show while they do. Until TASK-0313 they were at the upper right, under the command line.
    private void PlaceRows(Vector2 displaySize, LiveBody? body, IReadOnlyList<EdgeValues.Value>? values, IReadOnlyList<LiveBody> bodies, DocumentLine? document, IReadOnlyList<HistoryStep>? history = null)
    {
        (_rows, _rowsArea, _rowLines, _cutRow, _cutTools, _madeFrom, _rowsUnder, _hiddenTooRow) = ([], null, [], null, [], null, false, null);
        if (!RowsShow(displaySize, body) || body is null)
        {
            _renaming = null;
            return;
        }

        var left = 16f + _inset.X;
        var right = left + RowsWidth;
        var top = PanelTop;
        var bottom = displaySize.Y - 32f - _inset.W - AnchorSize;

        // ADR-0131 item 1 (phase R18k, TASK-0360): on a portrait display the rows are under the part, at the full width, with
        // their last row 8 pixels above the ring of the last frame (or above the anchors), as the render at 768 by 1024. They
        // take the room from the middle of the display down, so the part, which the camera frames at the middle, stays free.
        _rowsUnder = displaySize.X < displaySize.Y;
        if (_rowsUnder)
        {
            right = displaySize.X - 16f - _inset.Z;
            top = displaySize.Y / 2f;
            bottom = Math.Min(bottom, (RingTop() ?? float.MaxValue) - 8f);
            // ADR-0135 item 3 (phase R18o, TASK-0368): the sheet of a proposal is under the arc at the top, so the rows no
            // longer end above it, as ADR-0131 item 1 said.
        }
        // ADR-0117 item 1: the first row is one row when the state fits after the name, else the name and a line of the state.
        _titleHeight = RowTitle(body).OneLine ? RowHeight : RowHeight + ImGui.GetTextLineHeight();
        var y = top + _titleHeight;
        // The review of Codex of v0.252 (TASK-0271): the rows end 16 pixels above the anchors, as the panel does; a value
        // that has no room keeps its chip only. Until TASK-0271 a long list went past the bottom of the display.

        // ADR-0131 item 1 (phase R18k, TASK-0360): on a portrait display the room under the part is small, and the ring and the
        // command line hold no cut, so the row of a cut and the row of "Hidden too" come right after the first row and always
        // show, as the first row does; a value row that has no room keeps its chip. The review of the branch found that the
        // cut had no room on a phone with a ring and a value.
        _cutTools = [.. bodies.Where(other => other.Id != body.Id && other.Visible)];
        if (_rowsUnder && _cutTools.Count > 0)
        {
            _cutRow = new Vector4(left, y, right, y + RowHeight);
            y += RowHeight;
        }

        if (_rowsUnder && _dimensions)
        {
            _hiddenTooRow = new Vector4(left, y, right, y + RowHeight);
            y += RowHeight;
        }

        // ADR-0117 item 2 (ADR-0099 Q2): the row "Made from" with the sketch of the body: the first step of its history that
        // opens a sketch. A body that no sketch made, as a box, has no such row.
        if (SketchStep is { } sketchStep && y + RowHeight <= bottom)
        {
            _madeFrom = (sketchStep, new Vector4(left, y, right, y + RowHeight));
            y += RowHeight;
        }
        foreach (var value in values ?? [])
        {
            // ADR-0148 items 1 and 2 (phase R22b, TASK-0383): each value of the design has its row in each view; the reference
            // of a chain is not a value of the design, so it has no row.
            if (value.Body != body.Id || value.Reference)
                continue;
            if (y + RowHeight > bottom)
                break;
            _rows.Add((value, new Vector4(left, y, right, y + RowHeight)));
            y += RowHeight;
        }

        // ADR-0099 item 4: the row of the list of a cut, while another body is visible, as in the panel (ADR-0073 item 1).
        if (!_rowsUnder && _cutTools.Count > 0 && y + RowHeight <= bottom)
        {
            _cutRow = new Vector4(left, y, right, y + RowHeight);
            y += RowHeight;
        }

        // ADR-0131 item 3 (phase R18k, TASK-0360): the row of "Hidden too" while the dimensions show, where it has room.
        if (!_rowsUnder && _dimensions && y + RowHeight <= bottom)
        {
            _hiddenTooRow = new Vector4(left, y, right, y + RowHeight);
            y += RowHeight;
        }

        // ADR-0099 item 3: the lines that the panel gave until TASK-0313, each where it has room: the selected face, the
        // selected edge, the size while the dimensions show (ADR-0056 item 6), and the message of the document.
        var step = ImGui.GetTextLineHeight() + 12f;
        foreach (var (item, text) in new[] { (FaceLabel, _face?.Text), (EdgeLabel, _face?.EdgeText), (SizeLabel, _dimensions ? _size : null), (DocumentMessageLabel, document?.Message) })
        {
            if (text is null || y + step > bottom)
                continue;
            _rowLines.Add((item, text, new Vector4(left, y, right, y + step)));
            y += step;
        }

        _rowsArea = new Vector4(left, top, right, y);

        // ADR-0131 item 1: the rows under the part move down, so their last row is on the bottom of their room. The first rows
        // always show, so when the room is lower than it, it moves up above the bottom of the room, and not above the top of
        // the panel. Thus it covers no tool of the ring and no part of the sheet of a proposal.
        // ADR-0135 item 3 (phase R18o, TASK-0368): while a proposal waits on a portrait display, they do not go above the arc of
        // the last frame either, which is at the top.
        // Finding V190 of the codebase review of 2026-11-14 (d) (TASK-0374): the sheet of a portrait display ends at most 8 pixels
        // above the middle of the display (ADR-0135 item 3), so the rows do not go above the middle, also in the first frame of
        // a proposal. Until TASK-0374 the floor came from the sheet of the last frame, which the first frame does not have.
        var floor = _rowsUnder && _proposals.Count > 0 ? Math.Max(PanelTop, displaySize.Y / 2f) : PanelTop;
        if (_rowsUnder && y != bottom)
            Shift(Math.Max(bottom - y, floor - top));
        if (_renaming != body.Id)
            _renaming = null;
    }

    // ADR-0099 item 2: a tap on the first row, or the command Rename, opens the name field with the name of the body.
    private void StartRename(LiveBody body)
    {
        _name = body.Name ?? string.Empty;
        (_renaming, _focusRename) = (body.Id, true);
    }

    // ADR-0099 item 2: the name field in the place of the first row. Enter renames the body with one command; Escape, or a tap
    // that takes the keys from the field, closes it with no command. An open pad disables it, as each text field (ADR-0032
    // item 6).
    private void RenameField(LiveBody body, Vector4 title)
    {
        ImGui.SetNextWindowPos(new Vector2(title.X, title.Y), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("##Rename", FixedFlags))
        {
            if (_focusRename)
            {
                ImGui.SetKeyboardFocusHere();
                _focusRename = false;
            }

            ImGui.SetNextItemWidth(title.Z - title.X);
            ImGui.BeginDisabled(_pad is not null);
            var enter = ImGui.InputText("##RenameText", ref _name, 128, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.EndDisabled();
            var active = ImGui.IsItemActive();
            var left = ImGui.IsItemDeactivated();
            Item(RenameItem);
            if (enter)
            {
                _actions.Add(new RenameBodyAction(body.Id, _name));
                (_renaming, _dropFocus) = (null, true);
            }
            else if (left || (active && ImGui.IsKeyPressed(ImGuiKey.Escape)))
            {
                (_renaming, _dropFocus) = (null, true);
            }

            AddWindowRectangle();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    // ADR-0130 item 1 (phase R18i, TASK-0358): the gap between the crossed value now and the proposed value of a row, and the
    // two texts of each row that a proposal changes in this frame, for a test.
    private const float RowProposalGap = 10f;

    private readonly Dictionary<string, (string Now, string Proposed)> _rowProposals = new(StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, (string Now, string Proposed)> RowProposals => _rowProposals;

    // Items 2 and 3: the rows of this frame, and the tap on a value row.
    private void Rows(LiveBody? body)
    {
        if (body is null || _rowsArea is not { } area)
            return;

        // Item 3: a press and a release on one value row, with no window of the panels under the pointer and no pad open,
        // open its pad.
        var io = ImGui.GetIO();
        if (_pad is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease()
            && RowAt(io.MousePos) is { } released && RowAt(io.MouseClickedPos[0]) is { } pressed && pressed.Key == released.Key)
            Use(released);

        // ADR-0117 item 2: a press and a release on the row "Made from" open its sketch, as its node of the history arc does.
        if (_madeFrom is { } made && _pad is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease()
            && Inside(made.Rectangle, io.MousePos) && Inside(made.Rectangle, io.MouseClickedPos[0]))
            _actions.Add(new EditSketchAction(made.Step.Id));

        // ADR-0131 item 3 (phase R18k, TASK-0360): a press and a release on the row of "Hidden too" toggle it.
        if (_hiddenTooRow is { } hiddenToo && _pad is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease()
            && Inside(hiddenToo, io.MousePos) && Inside(hiddenToo, io.MouseClickedPos[0]))
            _hiddenToo = !_hiddenToo;

        // ADR-0099 item 2: a press and a release on the first row open the name field; a pointer over it gives the reason of a
        // fault, as the line of the fault in the panel did until TASK-0313.
        var titleArea = new Vector4(area.X, area.Y, area.Z, area.Y + _titleHeight);
        bool OnTitle(Vector2 point) => point.X >= titleArea.X && point.X < titleArea.Z && point.Y >= titleArea.Y && point.Y < titleArea.W;
        if (_pad is null && _renaming is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease()
            && OnTitle(io.MousePos) && OnTitle(io.MouseClickedPos[0]))
            StartRename(body);
        if (body.Fault is { } shownFault && _renaming is null && OnTitle(io.MousePos) && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow))
            ImGui.SetTooltip(shownFault.Message);

        // Item 2: no ground and no border; a line under each row. Finding V129 of the codebase review of 2026-11-07 (b)
        // (TASK-0271): the name is on the first line of the first row, and the kind and the state on its second line, each
        // in the width of the column; only "failed" and the code are red. Until TASK-0271 the state of a hidden body pushed
        // the name out to "..." and crossed the left edge of the column.
        var list = ImGui.GetBackgroundDrawList();
        var colours = ImGui.GetStyle().Colors;
        var text = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
        var quiet = ImGui.GetColorU32(colours[(int)ImGuiCol.TextDisabled]);
        var line = ImGui.GetColorU32(colours[(int)ImGuiCol.Separator]);
        var (left, top, right) = (area.X, area.Y, area.Z);
        var middle = (RowHeight - ImGui.GetTextLineHeight()) / 2f;
        var (name, parts, oneLine) = RowTitle(body);
        var lineHeight = ImGui.GetTextLineHeight();
        // ADR-0117 item 1: the name in the font of a title, drawn a second time 1 pixel to the right, because the binding draws
        // the weight 400 only; its state after it on the same line, on the line of the text under it, or under it.
        var nameAt = new Vector2(left, top + ((RowHeight - TitleSize) / 2f));
        if (_renaming == body.Id)
            RenameField(body, titleArea);
        else
        {
            list.AddText(_titleFont, TitleSize, nameAt, text, name);
            list.AddText(_titleFont, TitleSize, nameAt + new Vector2(1f, 0f), text, name);
        }

        var x = oneLine ? left + TitleWidth(name) + TitleGap : left;
        var stateY = oneLine ? nameAt.Y + TitleSize - lineHeight - 2f : top + RowHeight;
        foreach (var (part, red) in parts)
        {
            // ADR-0108 item 5 (phase R18a, TASK-0332): "correct" is green, as the render. ADR-0117 item 1: the kind too, as the
            // render gives "Extrude · correct" in green.
            var correct = body.Fault is null && !red;
            list.AddText(new Vector2(x, stateY), red ? FailedColour : correct ? Theme.Correct : quiet, part);
            x += ImGui.CalcTextSize(part).X;
        }
        list.AddLine(new Vector2(left, top + _titleHeight), new Vector2(right, top + _titleHeight), line, 1f);
        _items[RowsTitleItem] = titleArea;

        // ADR-0117 item 2: "Made from" in the quiet colour, and the name of the sketch in the accent at the right.
        if (_madeFrom is { } madeFrom)
        {
            var (min, max) = (new Vector2(madeFrom.Rectangle.X, madeFrom.Rectangle.Y), new Vector2(madeFrom.Rectangle.Z, madeFrom.Rectangle.W));
            if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) && Inside(madeFrom.Rectangle, io.MousePos))
                list.AddRectFilled(min, max, ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), 6f);
            list.AddText(new Vector2(min.X, min.Y + middle), quiet, MadeFromLabel);
            var sketch = Fit(madeFrom.Step.Text, RowsWidth - ImGui.CalcTextSize(MadeFromLabel).X - TitleGap);
            list.AddText(new Vector2(max.X - ImGui.CalcTextSize(sketch).X, min.Y + middle), Theme.Accent, sketch);
            list.AddLine(new Vector2(min.X, max.Y), max, line, 1f);
            _items[MadeFromItem] = madeFrom.Rectangle;
        }

        // ADR-0130 item 1 (phase R18i, TASK-0358): while the window shows a proposal, a row whose value the proposal changes
        // gives the value now crossed out in the quiet colour and the proposed value in amber at the right, as the render.
        var proposed = _proposals.Count > 0 && _proposals[0].Values is { } commands ? ProposedValues.Of(commands) : [];
        _rowProposals.Clear();
        foreach (var (value, rectangle) in _rows)
        {
            var (min, max) = (new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.Z, rectangle.W));
            if (proposed.FirstOrDefault(change => change.Keys.Contains(value.Key)) is { } change)
            {
                var next = $"{EdgeValues.Exact(change.Shown ?? change.Number)} {change.Unit ?? _unit}";
                var nextWidth = _monoFont.CalcTextSizeA(FontSize, float.MaxValue, 0f, next).X;
                var nowWidth = _monoFont.CalcTextSizeA(FontSize, float.MaxValue, 0f, value.Text).X;
                var nowAt = new Vector2(max.X - nextWidth - RowProposalGap - nowWidth, min.Y + middle);
                list.AddText(new Vector2(min.X, min.Y + middle), text, value.Label);
                if (value.Name is { } proposedName)
                    list.AddText(new Vector2(min.X + ImGui.CalcTextSize(value.Label).X + 8f, min.Y + middle), quiet, proposedName);
                list.AddText(_monoFont, FontSize, nowAt, quiet, value.Text);
                var strike = nowAt.Y + (FontSize / 2f);
                list.AddLine(new Vector2(nowAt.X, strike), new Vector2(nowAt.X + nowWidth, strike), quiet, 1.5f);
                list.AddText(_monoFont, FontSize, new Vector2(max.X - nextWidth, min.Y + middle), ImGui.GetColorU32(DimensionColour), next);
                list.AddLine(new Vector2(min.X, max.Y), max, line, 1f);
                _items[RowItem(value.Key)] = rectangle;
                _rowProposals[value.Key] = (value.Text, next);
                continue;
            }

            var pending = value.Key == _pendingValue;
            var hovered = !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) && RowAt(io.MousePos)?.Key == value.Key;
            if (hovered)
                list.AddRectFilled(min, max, ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), 6f);
            if (pending)
                Dashed(list, min, max, DimensionColour);
            // ADR-0117 item 3: the label in the colour of the text, the name of its constraint in the quiet colour after it, and
            // the value in the mono font of a value at the right.
            list.AddText(new Vector2(min.X, min.Y + middle), text, value.Label);
            if (value.Name is { } constraintName)
                list.AddText(new Vector2(min.X + ImGui.CalcTextSize(value.Label).X + 8f, min.Y + middle), quiet, constraintName);
            var width = _monoFont.CalcTextSizeA(FontSize, float.MaxValue, 0f, value.Text).X;
            list.AddText(_monoFont, FontSize, new Vector2(max.X - width, min.Y + middle), pending ? DimensionColour : text, value.Text);
            list.AddLine(new Vector2(min.X, max.Y), max, line, 1f);
            _items[RowItem(value.Key)] = rectangle;
        }

        // ADR-0099 item 4: the row of the list of a cut.
        if (_cutRow is { } cut)
            CutRow(body, cut);

        // ADR-0131 item 3: "Hidden too" in the colour of the text, and its state "on" in the accent or "off" in the quiet colour
        // at the right.
        if (_hiddenTooRow is { } hiddenTooRow)
        {
            var (min, max) = (new Vector2(hiddenTooRow.X, hiddenTooRow.Y), new Vector2(hiddenTooRow.Z, hiddenTooRow.W));
            if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) && Inside(hiddenTooRow, io.MousePos))
                list.AddRectFilled(min, max, ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), 6f);
            list.AddText(new Vector2(min.X, min.Y + middle), text, HiddenTooLabel);
            var state = _hiddenToo ? "on" : "off";
            list.AddText(new Vector2(max.X - ImGui.CalcTextSize(state).X, min.Y + middle), _hiddenToo ? Theme.Accent : quiet, state);
            list.AddLine(new Vector2(min.X, max.Y), max, line, 1f);
            _items[HiddenTooItem] = hiddenTooRow;
        }

        // ADR-0099 item 3: the lines under the values, in the quiet colour, with no tap.
        foreach (var (item, shown, rectangle) in _rowLines)
        {
            list.AddText(new Vector2(rectangle.X, rectangle.Y + 6f), quiet, Fit(shown, RowsWidth));
            _items[item] = rectangle;
        }
    }

    // ADR-0099 item 4: the label of the row, and at its right the list of the bodies that can cut the selected body; a choice
    // gives the command Subtract, as the list of the panel does. The open list belongs to the panels (V116).
    private void CutRow(LiveBody body, Vector4 row)
    {
        var width = 160f;
        ImGui.SetNextWindowPos(new Vector2(row.X, row.Y + ((RowHeight - ImGui.GetFrameHeight()) / 2f)), ImGuiCond.Always);
        // ADR-0131 item 1 (phase R18k, TASK-0360): the window of the row has no padding, but the open list keeps the padding of
        // the style, as the list of the panel did, so each entry is inside the list. A window reads the padding at its Begin.
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        var shown = ImGui.Begin("##RowsCut", FixedFlags);
        ImGui.PopStyleVar();
        if (shown)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled(CutLabel);
            ImGui.SameLine(row.Z - row.X - width);
            ImGui.SetNextItemWidth(width);
            if (ImGui.BeginCombo($"##{CutLabel}", "Choose"))
            {
                foreach (var tool in _cutTools)
                {
                    if (ImGui.Selectable($"{RowText(tool)}##cut{tool.Id}", false, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                        _actions.Add(new CutBodyAction(body.Id, tool.Id));
                    Item(CutItem(tool.Id));
                }

                var (at, extent) = (ImGui.GetWindowPos(), ImGui.GetWindowSize());
                _cutList = new Vector4(at.X, at.Y, at.X + extent.X, at.Y + extent.Y);
                ImGui.EndCombo();
            }

            Item(CutLabel);
            AddWindowRectangle();
        }

        ImGui.End();
        if (_cutList is { } open)
            _panels.Add(open);
        var line = ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Separator]);
        ImGui.GetBackgroundDrawList().AddLine(new Vector2(row.X, row.W), new Vector2(row.Z, row.W), line, 1f);
    }

    // V129 (TASK-0271): the name of the first row, and the parts of its second line: the kind, "failed" and the code (red),
    // or "correct", and "hidden"; each line fits the width of the column. A state that is too wide loses the kind first,
    // then "hidden", so that "failed" and the code stay whole; the panel gives the kind and the visibility too.
    // ADR-0117 item 1 (TASK-0342): the name is in the font of a title, and OneLine is true when the state fits after it.
    internal (string Name, IReadOnlyList<(string Text, bool Red)> State, bool OneLine) RowTitle(LiveBody body)
    {
        var (name, state) = TitleParts(body);
        return (name, state, TitleWidth(name) + TitleGap + ImGui.CalcTextSize(string.Concat(state.Select(part => part.Text))).X <= RowsWidth);
    }

    // The width of a text in the font of a title.
    private float TitleWidth(string text) => _titleFont.CalcTextSizeA(TitleSize, float.MaxValue, 0f, text).X;

    private static bool Inside(Vector4 rectangle, Vector2 point)
        => point.X >= rectangle.X && point.X < rectangle.Z && point.Y >= rectangle.Y && point.Y < rectangle.W;

    private (string Name, IReadOnlyList<(string Text, bool Red)> State) TitleParts(LiveBody body)
    {
        var name = FitTitle(body.Name ?? body.Kind, RowsWidth);
        var state = body.Fault is { } fault ? ($"failed {fault.Code}", true) : ("correct", false);
        List<(string Text, bool Red)>[] forms =
        [
            [($"{body.Kind} · ", false), state, .. body.Visible ? Array.Empty<(string, bool)>() : [(" · hidden", false)]],
            [state, .. body.Visible ? Array.Empty<(string, bool)>() : [(" · hidden", false)]],
            [state],
        ];
        foreach (var form in forms)
        {
            if (ImGui.CalcTextSize(string.Concat(form.Select(part => part.Text))).X <= RowsWidth)
                return (name, form);
        }

        return (name, [(Fit(state.Item1, RowsWidth), state.Item2)]);
    }

    // ADR-0117 item 1: the text, or its start and "...", in the width, in the font of a title.
    private string FitTitle(string text, float width)
    {
        if (TitleWidth(text) <= width)
            return text;
        for (var length = text.Length - 1; length > 0; length--)
        {
            var cut = text[..length] + "...";
            if (TitleWidth(cut) <= width)
                return cut;
        }

        return "...";
    }

    // The text, or its start and "...", in the width.
    internal static string Fit(string text, float width)
    {
        if (ImGui.CalcTextSize(text).X <= width)
            return text;
        for (var length = text.Length - 1; length > 0; length--)
        {
            var cut = text[..length] + "...";
            if (ImGui.CalcTextSize(cut).X <= width)
                return cut;
        }

        return "...";
    }

    // Item 4: a chip that the placement put on the rows goes under them, at the same x, so that a value shows once in
    // each place and a tap finds one of them. ADR-0131 item 2 (phase R18k, TASK-0360): above them when the rows are under the
    // part, because the ring and the anchors are under the rows.
    private Vector4 OutOfRows(Vector4 chip)
    {
        if (_rowsArea is not { } area || chip.Z <= area.X || chip.X >= area.Z || chip.W <= area.Y || chip.Y >= area.W)
            return chip;
        var move = _rowsUnder ? area.Y - 8f - chip.W : area.W + 8f - chip.Y;
        return chip with { Y = chip.Y + move, W = chip.W + move };
    }

    // ADR-0131 item 1: moves each rectangle of the rows of this frame down by the distance.
    private void Shift(float down)
    {
        static Vector4 By(Vector4 rectangle, float distance) => rectangle with { Y = rectangle.Y + distance, W = rectangle.W + distance };
        _rows = [.. _rows.Select(row => (row.Value, By(row.Rectangle, down)))];
        _rowLines = [.. _rowLines.Select(row => (row.Item, row.Text, By(row.Rectangle, down)))];
        if (_cutRow is { } cut)
            _cutRow = By(cut, down);
        if (_madeFrom is { } made)
            _madeFrom = (made.Step, By(made.Rectangle, down));
        if (_hiddenTooRow is { } hiddenToo)
            _hiddenTooRow = By(hiddenToo, down);
        if (_rowsArea is { } area)
            _rowsArea = By(area, down);
    }
}
