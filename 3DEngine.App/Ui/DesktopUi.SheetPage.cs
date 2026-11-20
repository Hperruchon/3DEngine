using System.Numerics;
using Engine.Core.Export;
using Engine.Core.Queries;
using ImGuiNET;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0168 (phase R30c, TASK-0408): the sheet page. A button "Sheet" beside "Views" opens it; it replaces the scene with the
// paper of the first live sheet, its frame, its title block, its views and its values, from GetSheet. A tap on a view selects
// it and gives "Go to", "Scale", "Remove" and the switches of its lines; a drag moves it with the alignment of the projection;
// a tap on the free paper places a new view. The page holds the keys and the pointer while it shows.
internal sealed unsafe partial class DesktopUi
{
    public const string SheetLabel = "Sheet";
    public const string PageCloseLabel = "3D";
    public const string KeepAlignedLabel = "Keep aligned";
    public const string GoToLabel = "Go to";
    public const string ScaleViewLabel = "Scale";
    public const string RemoveViewLabel = "Remove";
    public const string HiddenLinesLabel = "Hidden lines";
    public const string CentreLinesLabel = "Centre lines";
    public const string PlaceCancelLabel = "Cancel";
    public const string PlaceHint = "Place a view here:";
    public const string PageReading = "Reading the sheet…";

    // ADR-0171 items 2 to 4 (TASK-0413): "Section", its looks in their order, its "Cancel" and the two hints of its row.
    public const string SectionLabel = "Section";
    public static readonly IReadOnlyList<string> SectionLooks = ["Up", "Down", "Left", "Right"];
    public const string SectionCancelLabel = "Cancel section";
    public const string SectionCutHint = "Tap the view at the cut:";
    public const string SectionPlaceHint = "Tap the paper for the section:";

    // The item of a look of the section, for ItemRectangle.
    public static string SectionLookItem(string look) => PageItem($"Look {look}");

    // ADR-0170 item 1 (phase R30d, TASK-0412): "Export" of the page, and the word of each format in the command line.
    public const string ExportSheetLabel = "Export";

    public static string ExportLabel(SheetFormat format) => $"Export {SheetExports.Label(format)}";

    // True while the row of the formats shows under the header of the page.
    private bool _exportOpen;

    internal bool ExportOpen => _exportOpen;

    // The item of a control of the page, for ItemRectangle.
    public static string PageItem(string label) => $"Page:{label}";

    // ADR-0168 item 1: true while the page shows; the sheet that it shows; and the read that the host gives before each frame.
    private bool _pageOpen;
    private Guid? _pageSheetId;
    private SheetInfo? _pageRead;
    private bool _pagePending;

    // Finding V206 of the codebase review of 2026-11-18 (b) (TASK-0416): the place and the two flags of each view as the page
    // sent them last. They stay until a read comes that is not pending after the send: the host gives a pending read while a
    // command waits or runs and until the read of its version ends. The page shows each view with them, so a second change
    // before the read starts from the first one. Until TASK-0416 each change started from the read, so a second switch sent
    // the first flag back, and a view went back to its old place until the read came.
    private readonly Dictionary<Guid, (double X, double Y, bool Hidden, bool Centre)> _pageSent = [];
    private bool _pageSentSeen;
    private SheetInfo? _pageShown;

    // ADR-0177 items 2 and 3 (phase R30f, TASK-0422): the sheet items of the waiting proposal as the host read them, or null; and
    // the proposal whose sheet the page took last.
    private SheetProposal? _sheetProposal;
    private Guid? _sheetProposalSeen;

    internal SheetProposal? SheetProposalShown => _sheetProposal;

    // ADR-0177 item 3: the boxes of the proposed views and the places of the proposed values on the screen in the last frame, for
    // a test: the rectangle (left, top, right, bottom) of each box, and the pixel of the middle of the base line of each value.
    internal IReadOnlyList<(Guid View, Vector4 Rectangle)> ProposedBoxes { get; private set; } = [];

    internal IReadOnlyList<(Guid Dimension, string Text, Vector2 At)> ProposedTexts { get; private set; } = [];

    // ADR-0177 item 3: a new proposal with sheet items takes the page to its sheet once; the person can choose another sheet
    // after it. The selection, the place of a tap, a section and the row of the formats end with it.
    public void ShowSheetProposal(SheetProposal? proposal)
    {
        _sheetProposal = proposal;
        if (proposal is not null && proposal.Proposal != _sheetProposalSeen)
        {
            (_pageSheetId, _pageView, _pagePlace, _pageSection, _exportOpen, _pageShown) = (proposal.SheetId, null, null, null, false, null);
            _sheetProposalSeen = proposal.Proposal;
        }
    }

    // ADR-0177 item 5: true while the page shows the sheet of the proposal of the card.
    internal bool ProposalOnPage(ProposalCard card)
        => PageOpen && _sheetProposal is { } proposal && proposal.Proposal == card.Id && PageSheetId == proposal.SheetId;

    // ADR-0177 item 4: true while the page shows the new sheet of a proposal, which is not in the Document. The host then reads no
    // GetSheet, and the page takes no tap on the paper.
    internal bool PageProposedNew => _sheetProposal?.NewSheet is { } proposed && PageSheetId == proposed.SheetId;

    // The read of the page with the values that the page sent last. ADR-0177 item 4: the paper of the new sheet of a proposal.
    private SheetInfo? ShownPage => PageProposedNew ? _sheetProposal!.NewSheet : _pageShown ??= _pageRead is { } read && _pageSent.Count > 0
        ? read with { Views = [.. read.Views.Select(view => _pageSent.TryGetValue(view.ViewId, out var sent) ? SheetPage.Sent(view, sent.X, sent.Y, sent.Hidden, sent.Centre) : view)] }
        : _pageRead;

    private void PageSend(SheetViewInfo view, double x, double y, bool hidden, bool centre)
    {
        _pageSent[view.ViewId] = (x, y, hidden, centre);
        (_pageSentSeen, _pageShown) = (false, null);
    }

    // Finding V207 (TASK-0416): the top of the rows of the header that change, the actions of a view, the views to place, a
    // section and the formats, in the last frame; null before the first read.
    private float? _pageStateTop;

    // ADR-0168 items 2, 3 and 5: the selected view, the place of a tap on the free paper, "Keep aligned", and a drag.
    private Guid? _pageView;
    private (double X, double Y)? _pagePlace;
    private bool _keepAligned = true;
    private (Guid View, Vector2 From, bool Moving)? _pageDrag;

    // ADR-0168 item 3 (finding V202 of the codebase review of 2026-11-18 (b), TASK-0415): true from a read without the dragged
    // view to the release, which then gives nothing.
    private bool _pageDragLost;
    private bool _pointerShift;

    // ADR-0171 items 2 to 4: the open section: its view, its look in the word of the command, and the place of its cut across its
    // line on the paper, or null before the tap on the view.
    private (Guid View, string Look, double? Cut)? _pageSection;

    public bool PageOpen => _pageOpen && _home is null;

    // The sheet that the page shows: the sheet that the person chose, or the first live sheet.
    public Guid? PageSheetId => _pageSheetId ?? _sheet?.Id;

    internal Guid? PageView => _pageView;

    internal (double X, double Y)? PagePlace => _pagePlace;

    internal bool KeepAligned => _keepAligned;

    internal (Guid View, string Look, double? Cut)? PageSection => _pageSection;

    // True while a drag moves a view, for a test.
    internal bool PageDragging => _pageDrag is { Moving: true };

    // The paper of the last frame on the screen, for a test.
    internal SheetPage.Paper? PagePaper { get; private set; }

    // ADR-0177 item 5 (TASK-0422): true when the window of the page is the window under the pointer in this frame.
    private bool _pageHovered;

    // The rectangle of the page in the last frame, which holds the pointer.
    private Vector4? _pageRectangle;

    public void ShowPage(SheetInfo? sheet, bool pending)
    {
        (_pageRead, _pagePending, _pageShown) = (sheet, pending, null);
        // Finding V206: a pending read after the last send, then a read that is not pending, ends the values that the page sent.
        if (pending)
            _pageSentSeen = true;
        else if (_pageSentSeen)
        {
            _pageSent.Clear();
            _pageSentSeen = false;
        }
    }

    internal void OpenPage() => _pageOpen = true;

    private void ClosePage()
    {
        (_pageOpen, _pageView, _pagePlace, _pageDrag, _pageDragLost, _exportOpen, _pageSection) = (false, null, null, null, false, false, null);
        _pageSent.Clear();
        (_pageSentSeen, _pageShown) = (false, null);
    }

    // ADR-0168 item 1: the button "Sheet" beside "Views" while a live sheet exists; a tap opens or closes the page.
    // ADR-0177 item 3 (TASK-0422): also while the waiting proposal has sheet items, with an amber outline.
    private void SheetButton()
    {
        if (_sheet is null && _sheetProposal is null)
            return;
        ImGui.SameLine(0f, 8f);
        var open = _pageOpen;
        if (open)
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertU32ToFloat4(Theme.Accent) with { W = 0.3f });
        if (ImGui.Button(SheetLabel, new Vector2(0f, AnchorSize)))
        {
            if (open)
                ClosePage();
            else
                (_pageOpen, _viewsOpen) = (true, false);
        }

        if (open)
            ImGui.PopStyleColor();
        Item(SheetLabel);
        if (_items.TryGetValue(SheetLabel, out var button))
        {
            var outline = _sheetProposal is not null ? DimensionColour : ImGui.GetColorU32(open ? ImGuiCol.ButtonActive : ImGuiCol.Border);
            ImGui.GetWindowDrawList().AddRect(new Vector2(button.X + 1f, button.Y + 1f), new Vector2(button.Z - 1f, button.W - 1f), outline, ImGui.GetStyle().FrameRounding, ImDrawFlags.None,
                _sheetProposal is not null ? Theme.InUseWidth : Theme.OutlineWidth);
        }
    }

    // ADR-0168 items 1 and 6: the keys of the page: Escape ends the place of a tap, then the selection, then the page. The page
    // holds each key, so no key reaches the intents of the scene behind it.
    // Finding V203 (TASK-0416): padHolds is true when a field or a pad held the keys at the start of the frame.
    private void WatchPageKeys(IReadOnlyList<InputSample> samples, bool padHolds)
    {
        foreach (var sample in samples)
        {
            if (sample is PointerSample pointer)
                _pointerShift = (pointer.Modifiers & KeyModifiers.Shift) != 0;
        }

        if (!PageOpen || padHolds)
            return;
        if (samples.Any(sample => sample is KeySample { Down: true, Key: InputKey.Escape }))
        {
            // ADR-0170 item 1 (TASK-0412): the row of the formats closes first. ADR-0171 item 2 (TASK-0413): then a section.
            // Finding V205 (TASK-0416): only one of the two is open, and the order is the order of the comment.
            if (_exportOpen)
                _exportOpen = false;
            else if (_pageSection is not null)
                _pageSection = null;
            else if (_pagePlace is not null)
                _pagePlace = null;
            else if (_pageView is not null)
                _pageView = null;
            else
                ClosePage();
        }
    }

    // ADR-0168 item 1: the page in the free part of the scene of the last frame, under each other window of the document.
    private void PageWindow(Vector2 displaySize)
    {
        PagePaper = null;
        _pageRectangle = null;
        if (!PageOpen)
            return;
        (ProposedBoxes, ProposedTexts, _pageHovered) = ([], [], false);
        // ADR-0177 item 3 (TASK-0422): the page stays open on the new sheet of a proposal.
        if (PageSheetId is null)
        {
            ClosePage();
            return;
        }

        var room = FreePixels(_lastPanels.Concat(_lastFixed)) ?? new Vector4(0f, 0f, displaySize.X, displaySize.Y);
        _pageRectangle = room;
        ImGui.SetNextWindowPos(new Vector2(room.X, room.Y), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(room.Z - room.X, room.W - room.Y), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f));
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoBackground;
        if (ImGui.Begin("##SheetPage", flags))
        {
            _pageHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
            // The ground of the scene under the page, so no body shows through it.
            ImGui.GetBackgroundDrawList().AddRectFilled(Vector2.Zero, displaySize, Theme.Plate(1f));
            // Finding V204 of the codebase review of 2026-11-18 (b) (TASK-0416): a read without the view of the open section or
            // of the selection ends them. Until TASK-0416 a section of a removed view stayed hidden and blocked each drag.
            if (ShownPage is { } current && current.SheetId == PageSheetId)
            {
                if (_pageSection is { } open && current.Views.All(view => view.ViewId != open.View))
                    _pageSection = null;
                if (_pageView is { } selected && current.Views.All(view => view.ViewId != selected))
                    _pageView = null;
            }

            var width = ImGui.GetContentRegionAvail().X;
            _pageStateTop = null;
            PageHeader();
            // The paper keeps its place while the rows of the header change: it starts under room for three rows of the header,
            // or under the header when the header is higher.
            // Finding V207 of the codebase review of 2026-11-18 (b) (TASK-0416): and under room for the highest of the rows that
            // change, at the width of the page. Until TASK-0416 a phone of 390 pixels needed four rows for the actions of a view,
            // and the paper went 54 pixels down at the first tap.
            var row = MinimumControlHeight + ImGui.GetStyle().ItemSpacing.Y;
            var top = Math.Max(ImGui.GetCursorScreenPos().Y, room.Y + 8f + (3f * row));
            if (_pageStateTop is { } stateTop)
                top = Math.Max(top, stateTop + (PageStateRows(width) * row));
            var paperRoom = new Vector4(room.X, top, room.Z, room.W);
            if (ShownPage is { } sheet && sheet.SheetId == PageSheetId)
                PagePaperDraw(sheet, paperRoom);
            else
                ImGui.TextUnformatted(PageReading);
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    // ADR-0168 items 2, 3 and 5: the rows of the page: "3D", "Keep aligned" and "Reading…"; then the actions of a selected view,
    // or the views to place at a tap on the free paper.
    private void PageHeader()
    {
        var height = new Vector2(0f, MinimumControlHeight);
        if (ImGui.Button($"{PageCloseLabel}##Page", height))
            ClosePage();
        Item(PageItem(PageCloseLabel));
        SameLineIfFits(CheckboxWidth(KeepAlignedLabel));
        ImGui.Checkbox($"{KeepAlignedLabel}##Page", ref _keepAligned);
        Item(PageItem(KeepAlignedLabel));
        // With several live sheets, a button of each sheet chooses the sheet of the page. ADR-0177 item 3 (TASK-0422): the new
        // sheet of a waiting proposal has a button too, with an amber text.
        var choices = _liveSheets.Select(live => (live.Id, live.Name, Proposed: false)).ToList();
        if (_sheetProposal?.NewSheet is { } proposedSheet)
            choices.Add((proposedSheet.SheetId, proposedSheet.Name, true));
        if (choices.Count > 1)
        {
            foreach (var (choice, name, proposed) in choices)
            {
                SameLineIfFits(ButtonWidth(name));
                var chosen = choice == PageSheetId;
                if (chosen)
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertU32ToFloat4(Theme.Accent) with { W = 0.3f });
                if (proposed)
                    ImGui.PushStyleColor(ImGuiCol.Text, DimensionColour);
                // Finding V204 (TASK-0416): another sheet also ends the section and the row of the formats.
                if (ImGui.Button($"{name}##PageSheet{choice:N}", height))
                    (_pageSheetId, _pageView, _pagePlace, _pageSection, _exportOpen) = (choice, null, null, null, false);
                if (proposed)
                    ImGui.PopStyleColor();
                if (chosen)
                    ImGui.PopStyleColor();
                Item(PageItem($"Sheet {choice:N}"));
            }
        }
        else if (ShownPage is { } named)
        {
            SameLineIfFits(ImGui.CalcTextSize(named.Name).X);
            ImGui.TextUnformatted(named.Name);
        }

        // Finding V207 (TASK-0416): the mark of a pending read keeps its room when it does not show, so the rows of the header
        // do not change with it.
        SameLineIfFits(ImGui.CalcTextSize("…").X);
        if (_pagePending)
            ImGui.TextUnformatted("…");
        else
            ImGui.Dummy(ImGui.CalcTextSize("…"));

        // ADR-0177 item 4 (TASK-0422): the new sheet of a proposal has no export, no view to select and no place to tap.
        if (ShownPage is not { } sheet || PageProposedNew)
            return;

        // ADR-0170 item 1: "Export" opens a row of the formats, PDF first, in the place of the actions of a view and of the views
        // to place; a format closes the row.
        SameLineIfFits(ButtonWidth(ExportSheetLabel));
        var exporting = _exportOpen;
        if (exporting)
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertU32ToFloat4(Theme.Accent) with { W = 0.3f });
        // Finding V205 (TASK-0416): "Export" also ends a section, so only one of the two is open (ADR-0170 item 1).
        if (ImGui.Button($"{ExportSheetLabel}##Page", height))
            (_exportOpen, _pageView, _pagePlace, _pageSection) = (!_exportOpen, null, null, null);
        if (exporting)
            ImGui.PopStyleColor();
        Item(PageItem(ExportSheetLabel));
        _pageStateTop = ImGui.GetCursorScreenPos().Y;
        if (_exportOpen)
        {
            var firstFormat = true;
            foreach (var format in SheetExports.Formats)
            {
                var label = SheetExports.Label(format);
                if (!firstFormat)
                    SameLineIfFits(ButtonWidth(label));
                firstFormat = false;
                if (ImGui.Button($"{label}##Export", new Vector2(Math.Max(ButtonWidth(label), 72f), MinimumControlHeight)))
                {
                    _actions.Add(new SheetExportAction(sheet.SheetId, format));
                    _exportOpen = false;
                }

                Item(PageItem(ExportLabel(format)));
            }

            return;
        }

        // ADR-0171 items 2 to 4: the row of an open section: its hint, its looks and "Cancel section".
        if (_pageSection is { } section && sheet.Views.Any(view => view.ViewId == section.View))
        {
            ImGui.TextUnformatted(section.Cut is null ? SectionCutHint : SectionPlaceHint);
            foreach (var look in SectionLooks)
            {
                SameLineIfFits(ButtonWidth(look));
                var chosen = look.ToLowerInvariant() == section.Look;
                if (chosen)
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertU32ToFloat4(Theme.Accent) with { W = 0.3f });
                if (ImGui.Button($"{look}##Section", height))
                    _pageSection = section with { Look = look.ToLowerInvariant(), Cut = null };
                if (chosen)
                    ImGui.PopStyleColor();
                Item(SectionLookItem(look));
            }

            SameLineIfFits(ButtonWidth(SectionCancelLabel));
            if (ImGui.Button($"{SectionCancelLabel}##Section", height))
                _pageSection = null;
            Item(PageItem(SectionCancelLabel));
            return;
        }

        if (_pageView is { } id && sheet.Views.FirstOrDefault(view => view.ViewId == id) is { } selected)
        {
            var first = true;
            void Action(string label, Action press)
            {
                if (!first)
                    SameLineIfFits(ButtonWidth(label));
                first = false;
                if (ImGui.Button($"{label}##Page", height))
                    press();
                Item(PageItem(label));
            }

            var flat = SheetPage.FlatOf(selected.Direction);
            if (flat is { } goTo)
                Action(GoToLabel, () => { _actions.Add(new FlatViewAction(goTo)); _actions.Add(new FrameAction()); ClosePage(); });
            Action(ScaleViewLabel, () => OpenScalePad(sheet.SheetId, selected.ViewId, (selected.ScaleNumerator, selected.ScaleDenominator)));
            Action(RemoveViewLabel, () => { _actions.Add(new SheetRemoveAction(sheet.SheetId, selected.ViewId)); _pageView = null; });
            // Finding V208 of the codebase review of 2026-11-18 (b) (TASK-0416): "Section" shows for a view whose frame is a flat
            // view, as "Go to"; the section of such a parent has a flat frame too, so the engine takes it. Until TASK-0416 it
            // showed for each view, and the engine refused a pictorial parent after four taps.
            if (flat is not null)
                Action(SectionLabel, () => (_pageSection, _exportOpen) = ((selected.ViewId, "up", null), false));
            // Finding V206 (TASK-0416): the switches start from the values that the page sent last, and each change is kept.
            var (hidden, centre) = (selected.HiddenLines, selected.CentreLines);
            SameLineIfFits(CheckboxWidth(HiddenLinesLabel));
            if (ImGui.Checkbox($"{HiddenLinesLabel}##Page", ref hidden))
            {
                _actions.Add(new SheetLinesAction(sheet.SheetId, selected.ViewId, hidden, centre));
                PageSend(selected, selected.X, selected.Y, hidden, centre);
            }

            Item(PageItem(HiddenLinesLabel));
            SameLineIfFits(CheckboxWidth(CentreLinesLabel));
            if (ImGui.Checkbox($"{CentreLinesLabel}##Page", ref centre))
            {
                _actions.Add(new SheetLinesAction(sheet.SheetId, selected.ViewId, hidden, centre));
                PageSend(selected, selected.X, selected.Y, hidden, centre);
            }

            Item(PageItem(CentreLinesLabel));
        }
        else if (_pagePlace is { } place)
        {
            ImGui.TextUnformatted(PlaceHint);
            foreach (var view in Enum.GetValues<FlatView>())
            {
                var label = view.ToString();
                SameLineIfFits(ButtonWidth(label));
                if (ImGui.Button($"{label}##Place", height))
                {
                    _actions.Add(new SheetAddAtAction(sheet.SheetId, view, place.X, place.Y));
                    _pagePlace = null;
                }

                Item(PageItem($"Place {label}"));
            }

            SameLineIfFits(ButtonWidth(PlaceCancelLabel));
            if (ImGui.Button($"{PlaceCancelLabel}##Place", height))
                _pagePlace = null;
            Item(PageItem(PlaceCancelLabel));
        }
    }

    // ADR-0168 items 1 to 5: the paper and its drawing, and the tap and the drag on it.
    private void PagePaperDraw(SheetInfo sheet, Vector4 room)
    {
        var paper = SheetPage.Fit(sheet.Width, sheet.Height, room);
        PagePaper = paper;
        var list = ImGui.GetWindowDrawList();
        var rect = paper.Rectangle;
        list.AddRectFilled(new Vector2(rect.X, rect.Y), new Vector2(rect.Z, rect.W), Theme.Paper);

        // The tap and the drag: one invisible button over the paper.
        ImGui.SetCursorScreenPos(new Vector2(rect.X, rect.Y));
        ImGui.InvisibleButton("##Paper", new Vector2(Math.Max(1f, rect.Z - rect.X), Math.Max(1f, rect.W - rect.Y)));
        Item(PageItem("Paper"));
        var mouse = ImGui.GetIO().MousePos;
        // ADR-0177 item 4 (TASK-0422): the paper of the new sheet of a proposal takes no tap and no drag.
        var taps = !PageProposedNew;
        // ADR-0168 item 3 (finding V202, TASK-0415): a read without the dragged view ends the drag.
        if (_pageDrag is { } lost && sheet.Views.All(view => view.ViewId != lost.View))
            (_pageDrag, _pageDragLost) = (null, true);
        // ADR-0171 item 4 (TASK-0413): no drag while a section is open.
        // Finding V215 of the codebase review of 2026-11-20 (TASK-0426): a new press starts with no mark of a lost drag, so its
        // release acts. A frame with no paper ("Reading the sheet") ends the active item of a drag with no release, and a later
        // read without the dragged view then marked the drag as lost; until TASK-0426 the mark stayed until a release on the
        // paper, so the release of the next tap gave nothing.
        if (ImGui.IsItemActivated() && taps)
            (_pageDrag, _pageDragLost) = (_pageSection is null && SheetPage.Hit(sheet, paper.Millimetres(mouse)) is { } hit ? (hit, mouse, false) : null, false);
        (double X, double Y, bool Aligned)? moving = null;
        if (_pageDrag is { } drag && ImGui.IsItemActive())
        {
            var delta = mouse - drag.From;
            if (drag.Moving || delta.Length() > 4f)
            {
                _pageDrag = drag with { Moving = true };
                moving = SheetPage.Drag(sheet, drag.View, delta.X / paper.Scale, -delta.Y / paper.Scale, _keepAligned, _pointerShift);
            }
        }

        // Finding V221 of the codebase review of 2026-11-20 (TASK-0426): a tap whose press or release is on Reject or Accept of the
        // arc of the last frame is the tap of the arc only. Until TASK-0426 a tap on Accept over the paper also selected a view,
        // set the place of a tap, or placed a section.
        if (ImGui.IsItemDeactivated() && (OverLastProposal(mouse) || OverLastProposal(ImGui.GetIO().MouseClickedPos[0])))
            (_pageDrag, _pageDragLost) = (null, false);
        else if (ImGui.IsItemDeactivated() && _pageSection is { } open && sheet.Views.FirstOrDefault(view => view.ViewId == open.View) is { } cutView)
        {
            // ADR-0171 items 3 and 4: a tap on the view sets the cut; with a cut, a tap on the free paper places the section.
            var at = paper.Millimetres(mouse);
            var under = SheetPage.Hit(sheet, at);
            if (open.Cut is null && under == open.View)
                _pageSection = open with { Cut = SheetPage.CutPlace(cutView, open.Look, at) };
            else if (open.Cut is { } cut && under is null && at.X >= 0 && at.X <= sheet.Width && at.Y >= 0 && at.Y <= sheet.Height)
            {
                var vertical = open.Look is "left" or "right";
                var point = SheetPage.DesignPoint(cutView, vertical ? (cut, cutView.Y) : (cutView.X, cut));
                _actions.Add(new SheetSectionAction(sheet.SheetId, open.View, open.Look, point.X, point.Y, point.Z, at.X, at.Y));
                (_pageSection, _pageView) = (null, null);
            }

            (_pageDrag, _pageDragLost) = (null, false);
        }
        else if (ImGui.IsItemDeactivated() && taps)
        {
            var at = paper.Millimetres(mouse);
            if (_pageDragLost)
            {
                // The release of a drag that a read ended gives nothing.
            }
            else if (_pageDrag is { Moving: true } done
                && SheetPage.Drag(sheet, done.View, (mouse - done.From).X / paper.Scale, -(mouse - done.From).Y / paper.Scale, _keepAligned, _pointerShift) is var (x, y, aligned)
                && sheet.Views.FirstOrDefault(view => view.ViewId == done.View) is { } dragged
                && (x != dragged.X || y != dragged.Y))
            {
                // Finding E225 (TASK-0416): a drag that ends at the place of the view sends no move. Finding V206: the page keeps
                // the new place of the view and of each view that the move aligns, until the read comes.
                _actions.Add(new SheetMoveAction(sheet.SheetId, done.View, x, y, aligned));
                foreach (var view in sheet.Views.Where(view => aligned && OnLineOf(sheet, done.View, view)).ToList())
                    PageSend(view, view.X + (x - dragged.X), view.Y + (y - dragged.Y), view.HiddenLines, view.CentreLines);
                PageSend(dragged, x, y, dragged.HiddenLines, dragged.CentreLines);
                _pageView = done.View;
            }
            else if (SheetPage.Hit(sheet, at) is { } view)
                (_pageView, _pagePlace, _exportOpen) = (_pageView == view ? null : view, null, false);
            else if (at.X >= 0 && at.X <= sheet.Width && at.Y >= 0 && at.Y <= sheet.Height)
                (_pageView, _pagePlace, _exportOpen) = (null, at, false);
            (_pageDrag, _pageDragLost) = (null, false);
        }

        // The frame and the title block, as the files draw them.
        var (fx0, fy0, fx1, fy1) = (SvgExport.Margin, SvgExport.Margin, sheet.Width - SvgExport.Margin, sheet.Height - SvgExport.Margin);
        list.AddRect(paper.Pixel(fx0, fy1), paper.Pixel(fx1, fy0), Theme.Ink, 0f, ImDrawFlags.None, 2f);
        var (tx, ty) = (fx1 - SvgExport.TitleWidth, fy0);
        list.AddRect(paper.Pixel(tx, ty + SvgExport.TitleHeight), paper.Pixel(fx1, ty), Theme.Ink, 0f, ImDrawFlags.None, 2f);
        var font = ImGui.GetFont();
        float Size(double millimetres) => Math.Max(6f, (float)(millimetres * paper.Scale));
        // The texts of the title block stay inside it.
        list.PushClipRect(paper.Pixel(tx, ty + SvgExport.TitleHeight), paper.Pixel(fx1, ty), true);
        list.AddText(font, Size(5), paper.Pixel(tx + 3, ty + 17), Theme.Ink, sheet.Name);
        var projection = sheet.ThirdAngle ? "Third angle" : "First angle";
        list.AddText(font, Size(3.5), paper.Pixel(tx + 3, ty + 7.5), Theme.Ink, $"{sheet.Paper} {(sheet.Landscape ? "landscape" : "portrait")} · {projection} · Scale {SvgExport.Scale(sheet)} · mm");
        list.PopClipRect();

        // The views: hidden lines thin and dashed, centre lines thin with a long dash, visible lines thick.
        foreach (var view in sheet.Views)
        {
            var (ox, oy) = (0.0, 0.0);
            if (moving is { } m && _pageDrag is { } d && (view.ViewId == d.View || (m.Aligned && OnLineOf(sheet, d.View, view))))
            {
                var dragged = sheet.Views.First(candidate => candidate.ViewId == d.View);
                (ox, oy) = (m.X - dragged.X, m.Y - dragged.Y);
            }

            foreach (var line in view.Lines)
            {
                var points = SheetPage.Path(line).Select(p => paper.Pixel(p.U + ox, p.V + oy)).ToList();
                if (points.Count < 2)
                    continue;
                if (line.Kind is "centre" or "cut")
                {
                    foreach (var (a, b) in ViewLines.Dashes(points, 12f, 4f))
                        list.AddLine(a, b, Theme.Ink, HiddenLineWidth);
                }
                else if (line.Kind is "hatch" or "arrow")
                {
                    // ADR-0171 item 5 (TASK-0413): a hatch line and an arrow are thin and continuous; an arrow has its filled head.
                    var array = points.ToArray();
                    list.AddPolyline(ref array[0], array.Length, Theme.Ink, ImDrawFlags.None, HiddenLineWidth);
                    if (line.Kind == "arrow")
                    {
                        var head = SvgExport.Head(line);
                        list.AddTriangleFilled(paper.Pixel(head[0].X + ox, head[0].Y + oy), paper.Pixel(head[1].X + ox, head[1].Y + oy), paper.Pixel(head[2].X + ox, head[2].Y + oy), Theme.Ink);
                    }
                }
                else if (!line.Visible)
                {
                    foreach (var (a, b) in ViewLines.Dashes(points, HiddenDashOn, HiddenDashOff))
                        list.AddLine(a, b, Theme.Ink, HiddenLineWidth);
                }
                else
                {
                    var array = points.ToArray();
                    list.AddPolyline(ref array[0], array.Length, Theme.Ink, ImDrawFlags.None, VisibleLineWidth);
                }
            }

            // ADR-0171 item 5: the letters of a cut line and the label of a section, centred at their places.
            foreach (var text in view.Texts)
            {
                // Finding E224 (TASK-0416): the size of the font that gives a capital of text.Size, as in the SVG and the PDF file.
                var size = Size(SvgExport.FontSize(text.Size));
                var width = ImGui.CalcTextSize(text.Text).X * size / ImGui.GetFontSize();
                list.AddText(font, size, paper.Pixel(text.X + ox, text.Y + oy) - new Vector2(width / 2f, size), Theme.Ink, text.Text);
            }

            // ADR-0171 item 3: the cut of an open section across the box of its view, at the place of the pointer before the tap.
            if (_pageSection is { } shown && shown.View == view.ViewId)
            {
                var (x0, y0, x1, y1) = SheetPage.Box(view);
                var cut = shown.Cut ?? SheetPage.CutPlace(view, shown.Look, paper.Millimetres(mouse));
                var vertical = shown.Look is "left" or "right";
                var (from, to) = vertical ? (paper.Pixel(cut, y0 - 5), paper.Pixel(cut, y1 + 5)) : (paper.Pixel(x0 - 5, cut), paper.Pixel(x1 + 5, cut));
                list.AddLine(from, to, Theme.Accent, 2f);
            }

            if (view.ViewId == _pageView || (_pageDrag is { Moving: true } held && held.View == view.ViewId))
            {
                var (x0, y0, x1, y1) = SheetPage.Box(view);
                list.AddRect(paper.Pixel(x0 - 2 + ox, y1 + 2 + oy), paper.Pixel(x1 + 2 + ox, y0 - 2 + oy), Theme.Accent, 4f, ImDrawFlags.None, 2f);
            }
        }

        // The values of the dimensions at their places, as GetSheet gives them.
        foreach (var dimension in sheet.Dimensions)
        {
            var text = SvgExport.Value(dimension, "Ø");
            var size = Size(3.5);
            var width = ImGui.CalcTextSize(text).X * size / ImGui.GetFontSize();
            list.AddText(font, size, paper.Pixel(dimension.X, dimension.Y) - new Vector2(width / 2f, size), Theme.Ink, text);
        }

        // ADR-0177 item 3 (phase R30f, TASK-0422): the proposal of the agent over the sheet, in amber: an outline round the paper of a
        // new sheet, a box with its name above its upper left corner for each view, and each value at its place as the values of
        // the sheet.
        if (_sheetProposal is { } proposal && proposal.SheetId == sheet.SheetId)
        {
            if (PageProposedNew)
                list.AddRect(new Vector2(rect.X - 3f, rect.Y - 3f), new Vector2(rect.Z + 3f, rect.W + 3f), DimensionColour, 0f, ImDrawFlags.None, 2f);
            var boxes = new List<(Guid, Vector4)>(proposal.Boxes.Count);
            foreach (var box in proposal.Boxes)
            {
                var (from, to) = (paper.Pixel(box.X0, box.Y1), paper.Pixel(box.X1, box.Y0));
                list.AddRect(from, to, DimensionColour, 0f, ImDrawFlags.None, 2f);
                var size = Size(3.5);
                list.AddText(font, size, from - new Vector2(0f, size + 2f), DimensionColour, box.Name);
                boxes.Add((box.View, new Vector4(from.X, from.Y, to.X, to.Y)));
            }

            var texts = new List<(Guid, string, Vector2)>(proposal.Values.Count);
            foreach (var value in proposal.Values)
            {
                var size = Size(3.5);
                var width = ImGui.CalcTextSize(value.Text).X * size / ImGui.GetFontSize();
                var at = paper.Pixel(value.X, value.Y);
                list.AddText(font, size, at - new Vector2(width / 2f, size), DimensionColour, value.Text);
                texts.Add((value.Dimension, value.Text, at));
            }

            (ProposedBoxes, ProposedTexts) = (boxes, texts);
        }

        // The mark of a tap on the free paper.
        if (_pagePlace is { } place)
        {
            var at = paper.Pixel(place.X, place.Y);
            list.AddCircle(at, 8f, Theme.Accent, 0, 2f);
            list.AddLine(at - new Vector2(12f, 0f), at + new Vector2(12f, 0f), Theme.Accent, 2f);
            list.AddLine(at - new Vector2(0f, 12f), at + new Vector2(0f, 12f), Theme.Accent, 2f);
        }
    }

    // Finding V207 (TASK-0416): the count of the rows that the highest of the rows that change needs at the width: the formats,
    // a section, the actions of a view and the views to place. Each item goes on the line of the item before it when it fits,
    // as SameLineIfFits puts it.
    private int PageStateRows(float width)
    {
        static float Text(string text) => ImGui.CalcTextSize(text).X;
        IEnumerable<float>[] states =
        [
            SheetExports.Formats.Select(format => Math.Max(ButtonWidth(SheetExports.Label(format)), 72f)),
            [Math.Max(Text(SectionCutHint), Text(SectionPlaceHint)), .. SectionLooks.Select(ButtonWidth), ButtonWidth(SectionCancelLabel)],
            [ButtonWidth(GoToLabel), ButtonWidth(ScaleViewLabel), ButtonWidth(RemoveViewLabel), ButtonWidth(SectionLabel), CheckboxWidth(HiddenLinesLabel), CheckboxWidth(CentreLinesLabel)],
            [Text(PlaceHint), .. Enum.GetValues<FlatView>().Select(view => ButtonWidth(view.ToString())), ButtonWidth(PlaceCancelLabel)],
        ];
        var gap = ImGui.GetStyle().ItemSpacing.X;
        var most = 0;
        foreach (var items in states)
        {
            var (rows, used) = (0, -1f);
            foreach (var item in items)
            {
                if (used < 0 || width - (used + gap) < item)
                    (rows, used) = (rows + 1, item);
                else
                    used += gap + item;
            }

            most = Math.Max(most, rows);
        }

        return most;
    }

    // ADR-0168 item 3: a view on a line of projection through the dragged view, as MoveSheetViews takes it.
    private static bool OnLineOf(SheetInfo sheet, Guid dragged, SheetViewInfo view)
    {
        var moved = sheet.Views.First(candidate => candidate.ViewId == dragged);
        if (moved.ViewId == view.ViewId)
            return false;
        static bool Same(IReadOnlyList<double> a, IReadOnlyList<double> b, int i) => a[i] == b[i] && a[i + 1] == b[i + 1] && a[i + 2] == b[i + 2];
        return (Same(moved.Direction, view.Direction, 0) && moved.X == view.X) || (Same(moved.Direction, view.Direction, 3) && moved.Y == view.Y);
    }

    // ADR-0168 item 1: the page holds the pointer in its rectangle, so a tap on it does not reach the scene.
    private bool OverPage(double x, double y)
        => PageOpen && _pageRectangle is { } page && x >= page.X && x < page.Z && y >= page.Y && y < page.W;
}
