using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Input;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0174 (phase R31a, TASK-0419): the tools of the part on the ring with no selected body, their second level, and the file
// menu on the file name of the path line. The structure view, in the place of the panel, is in DesktopUi.Panel.cs.
internal sealed unsafe partial class DesktopUi
{
    // ADR-0174 item 1: the tools of the first level of the ring of the part.
    public const string PartSketchLabel = "Sketch";
    public const string BodiesLabel = "Bodies";
    public const string PartImagesLabel = "Images";

    // ADR-0178 item 1 (phase R20e, TASK-0424): the tool of the ring of the part while at least one live body is hidden.
    public const string ShowHiddenLabel = "Show hidden";

    // ADR-0174 item 1: the caption of a landscape ring of the part, after "TOOLS FOR".
    internal const string PartCaption = "the part";

    // ADR-0174 item 4: the item of the file menu, which is a window of its own.
    public const string FileMenuItem = "FileMenu";

    // ADR-0174 item 2: the level of the ring of the part.
    internal enum PartLevel
    {
        Tools,
        New,
        Sketch,
    }

    private PartLevel _partLevel = PartLevel.Tools;

    internal PartLevel PartLevelOfFrame => _partLevel;

    // ADR-0174 item 4: true while the file menu is open.
    private bool _fileMenuOpen;

    // Finding V211 (TASK-0426): true when the last frame drew the file menu.
    private bool _fileMenuShown;

    internal bool FileMenuOpen => _fileMenuOpen;

    // ADR-0174 item 4: true in the frame in which a release outside the open file menu closed it, so the ring of that frame
    // takes no tap from that release.
    private bool _menuTookRelease;

    // ADR-0174 item 4: open the file menu, as a tap on the file name does; for a test.
    internal void OpenFileMenu() => _fileMenuOpen = true;

    // Finding V213 of the codebase review of 2026-11-20 (TASK-0426): ADR-0174 item 4, a tap outside the open menu and the file
    // name closes the menu and gives no other action. True from such a press to its release.
    private bool _menuPress;

    // Finding V213: true when the press or the release of a pointer belongs to a tap outside the open menu and the file name of
    // the last frame. Route does not give such a press or release to the library, so no control takes it, also a control that
    // draws before the menu; the release closes the menu. Until TASK-0426 only the ring skipped that release, and Undo, the
    // structure view and "Views" took it.
    private bool FileMenuTakes(PointerSample pointer)
    {
        if (pointer.Phase == PointerPhase.Down && _fileMenuOpen && !OnFileMenu(pointer.X, pointer.Y))
            _menuPress = true;
        if (!_menuPress || pointer.Phase is not (PointerPhase.Down or PointerPhase.Up or PointerPhase.Cancel))
            return false;
        if (pointer.Phase is PointerPhase.Up or PointerPhase.Cancel)
            (_menuPress, _fileMenuOpen) = (false, false);
        return pointer.Phase != PointerPhase.Cancel;
    }

    // Finding V213: true when the point is on the open menu or on the file name of the last frame.
    private bool OnFileMenu(double x, double y)
    {
        bool On(string item) => _items.TryGetValue(item, out var box) && x >= box.X && x < box.Z && y >= box.Y && y < box.W;
        return On(FileMenuItem) || On(PathSegmentItem(PathLevel.Document));
    }

    // ADR-0174 item 2: the kinds of the second level of "New", in the order of the list "New" before R31a.
    internal static readonly IReadOnlyList<string> PartNewTools = [NewBoxLabel, NewCylinderLabel, NewConeLabel, NewSphereLabel, NewTorusLabel];

    // ADR-0174 item 1: true when the ring of this frame holds the tools of the part.
    private bool _partRing;

    // ADR-0174 item 1: true when the frame shows the ring of the part: no selected body, no active sketch, no open pad, no open
    // images, no sheet page, no home screen, no lines of a flat view and no open net of the views. On the light paper of the
    // lines of a flat view the labels of the ring do not read, and the ring covers the drawing. The open net covered the ring
    // at 640 by 360 and at 560 by 560.
    private bool PartRingShows(LiveBody? body)
        => body is null && !_sketchActive && _pad is null && !_imagesShown && !PageOpen && _home is null && _viewLines is null && !_viewsOpen;

    // ADR-0174 items 1 and 2: the labels of the ring of the part at its level.
    private List<string> PartRingLabels(IReadOnlyList<LiveBody> bodies)
    {
        // ADR-0184 item 1 (phase R26, TASK-0447): while a section lasts, the ring of the part holds the tools of the section.
        if (_section is not null)
            return SectionRingLabels();
        switch (_partLevel)
        {
            case PartLevel.New:
                return [.. PartNewTools, BackLabel];
            case PartLevel.Sketch:
                return [.. NewSketchButtons.Select(button => button.Label), SketchOnFaceLabel, BackLabel];
            default:
                var labels = new List<string> { NewLabel, PartSketchLabel };
                if (bodies.Count > 0)
                    labels.Add(BodiesLabel);
                // ADR-0184 item 1: "Section" after "Bodies", while the part has a live body.
                if (bodies.Count > 0)
                    labels.Add(SectionLabel);
                if (_imagesAvailable)
                    labels.Add(PartImagesLabel);
                // ADR-0178 item 1: "Show hidden" while at least one live body is hidden; with no hidden body it does not show.
                if (bodies.Any(body => !body.Visible))
                    labels.Add(ShowHiddenLabel);
                return labels;
        }
    }

    // ADR-0174 item 1: with no centre from the host, the middle of the scene less the fixed controls of the last frame, else of
    // the display. The structure view does not count, so the ring does not move when the view opens or folds.
    private Vector2 PartCentre(Vector2 displaySize)
    {
        if (FreePixels(_lastFixed) is { } free)
            return new Vector2((free.X + free.Z) / 2f, (free.Y + free.W) / 2f);
        return displaySize / 2f;
    }

    // ADR-0174 items 1 and 2: the tool of a segment of the ring of the part. A tool of the second level does what its control
    // of the panel did before R31a, and the ring goes back to the first level.
    private void UsePart(string label)
    {
        // ADR-0184 items 1 to 3: a tool of the section changes the section of the window only.
        if (UseSection(label))
            return;
        switch (label)
        {
            case NewLabel:
                _partLevel = PartLevel.New;
                return;
            case PartSketchLabel:
                _partLevel = PartLevel.Sketch;
                return;
            case BodiesLabel:
                _structureOpen = !_structureOpen;
                return;
            case PartImagesLabel:
                _imagesOpen = true;
                return;
            case BackLabel:
                _partLevel = PartLevel.Tools;
                return;
            case SketchOnFaceLabel:
                _actions.Add(new SketchOnFaceAction());
                break;
            // ADR-0178 item 1: the action of the button "Show all", which gives one SetBodyVisibility for each hidden body.
            case ShowHiddenLabel:
                _actions.Add(new ShowAllAction());
                break;
            default:
                var sketch = NewSketchButtons.FirstOrDefault(button => button.Label == label);
                if (PartNewTools.Contains(label))
                    OpenNew(label);
                else if (sketch.Label is not null)
                    _actions.Add(new NewSketchAction(sketch.Datum));
                break;
        }

        _partLevel = PartLevel.Tools;
    }

    // ADR-0174 item 4: the file menu under the file name of the path line, with the four file actions. A tap on an entry closes
    // the menu and opens the dialog of the system; a tap outside the menu and the file name closes it. A selected body, an open
    // pad or a host with no files closes it too.
    private void FileMenu(DocumentLine? document, LiveBody? body)
    {
        if (!_fileMenuOpen)
        {
            _fileMenuShown = false;
            return;
        }

        if (body is not null || _pad is not null || document is not { Files: true } || !_items.TryGetValue(PathSegmentItem(PathLevel.Document), out var file))
        {
            (_fileMenuOpen, _fileMenuShown) = (false, false);
            return;
        }

        ImGui.SetNextWindowPos(new Vector2(file.X, file.W + 4f), ImGuiCond.Always);
        // Finding V211 of the codebase review of 2026-11-20 (TASK-0426): the menu takes the focus in its first frame, so it is the
        // front window. Dear ImGui keeps the order of a window that shows again, and a tap brings a window to the front, so after
        // a tap on the structure view the menu opened behind it, and the view took the tap on "Open project...". The focus is
        // for the first frame only: a focus in each frame ends the press of each other window, also of the file name, which
        // then did not close the menu.
        if (!_fileMenuShown)
            ImGui.SetNextWindowFocus();
        _fileMenuShown = true;
        // The ground of a window of the theme lets the text under it show; the ground of the menu is opaque.
        ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg] with { W = 1f });
        var shown = ImGui.Begin($"###{FileMenuItem}", FileMenuFlags);
        ImGui.PopStyleColor();
        if (shown)
        {
            foreach (var (label, action) in new (string, PanelAction)[]
            {
                (OpenProjectLabel, new OpenProjectAction()), (SaveAsLabel, new SaveAsAction()),
                (ExportStlLabel, new ExportStlAction()), (ImportStlLabel, new ImportStlAction()),
            })
            {
                if (ImGui.Selectable(label, false, ImGuiSelectableFlags.None, new Vector2(FileMenuWidth, MinimumControlHeight)))
                {
                    _actions.Add(action);
                    _fileMenuOpen = false;
                }

                Item(label);
            }

            AddWindowRectangle();
            _items[FileMenuItem] = _fixed[^1];
        }

        ImGui.End();

        // A release outside the menu and the file name closes the menu, so the tap that opened it does not close it.
        var io = ImGui.GetIO();
        if (_fileMenuOpen && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !Inside(file, io.MousePos)
            && !(_items.TryGetValue(FileMenuItem, out var menu) && Inside(menu, io.MousePos)))
            (_fileMenuOpen, _menuTookRelease) = (false, true);
    }

    // ADR-0174 item 4: the file menu has the ground of a window, so its entries do not draw over the structure view or the part
    // under it. The first form had the flags of a fixed control, with no ground, and the real window showed the entries over
    // the rows of the open structure view.
    private const ImGuiWindowFlags FileMenuFlags = FixedFlags & ~ImGuiWindowFlags.NoBackground;

    // ADR-0174 item 4: the width of an entry of the file menu: the widest entry and a margin.
    private static float FileMenuWidth
        => new[] { OpenProjectLabel, SaveAsLabel, ExportStlLabel, ImportStlLabel }.Max(label => ImGui.CalcTextSize(label).X) + 24f;
}
