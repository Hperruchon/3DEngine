using System.Globalization;
using System.Numerics;
using Engine.Core.Preview;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0187 items 4 and 6 (phase R25, TASK-0462): the preview of the shown proposal on a landscape display of at least 1024 by
// 600 (desktop first): a panel at the lower right, above the status line, with the switch "Before, After, Both", the table
// "measure, before, after" of the volume of each body that the proposal adds or changes, the state of the solver after the
// commands for each sketch that it adds or changes, and the checks that change state. The panel is a state of the client
// (ADR-0007): the switch sends nothing to the engine. A portrait display, a small window and the sheet page show no panel,
// and the scene then draws the preview in the mode Both. The amber is the colour of a proposal (DimensionColour).
internal sealed unsafe partial class DesktopUi
{
    public const string PreviewWindowName = "Preview";
    public const string PreviewTitleText = "Preview of the proposal";
    public const string PreviewUpdatingText = "updating";
    public const string PreviewTitleItem = "Preview:title";

    // Item 6: the least display that shows the panel, and the most rows of each part of the panel.
    internal static readonly Vector2 PreviewLeast = new(1024f, 600f);
    public const int PreviewRows = 4;

    public static string PreviewSwitchItem(PreviewMode mode) => $"Preview:switch:{mode}";

    // Item 4: the word of each mode on the switch, in the order of the switch.
    internal static readonly (PreviewMode Mode, string Label)[] PreviewModes =
        [(PreviewMode.Before, "Before"), (PreviewMode.After, "After"), (PreviewMode.Both, "Both")];

    // The preview of this frame from the host, the mode of the switch, the proposal of the mode, and true when this frame
    // showed the panel.
    private PreviewCard? _preview;
    private PreviewMode _previewMode = PreviewMode.Both;
    private Guid? _previewProposal;
    private bool _previewShown;
    private readonly Dictionary<string, string> _previewTexts = new(StringComparer.Ordinal);

    // The text of each line of the panel of this frame, by its item, for a test.
    internal IReadOnlyDictionary<string, string> PreviewTexts => _previewTexts;

    // The rectangle of the panel of this frame, or null.
    internal Vector4? PreviewRectangle { get; private set; }

    // Item 4: the mode that the scene draws: the mode of the switch while the panel shows, else Both.
    internal PreviewMode PreviewMode => _previewShown ? _previewMode : PreviewMode.Both;

    // Item 3: the host gives the last preview of the shown proposal before each frame, or null. Another proposal starts in the
    // mode Both.
    public void ShowProposalPreview(PreviewCard? card)
    {
        _preview = card;
        if (card is not null && card.Proposal != _previewProposal)
            (_previewProposal, _previewMode) = (card.Proposal, PreviewMode.Both);
    }

    // Item 6: one line of the panel. A line with Before and After is a row of the table; a line with Quiet is in the colour of
    // a disabled text; a line with Amber is in the amber of a proposal.
    internal sealed record PreviewLine(string Item, string Text, string? Before = null, string? After = null, bool Amber = false, bool Quiet = false);

    // Item 6: the lines of the panel for a preview of the proposal, with the names of the live bodies of the frame.
    internal static IReadOnlyList<PreviewLine> PreviewLinesOf(PreviewCard card, ProposalCard proposal, IReadOnlyList<LiveBody> bodies, string unit)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(bodies);
        var lines = new List<PreviewLine>();
        if (card.Error is { } error)
        {
            lines.Add(new("Preview:failed", "The preview failed", Amber: true));
            lines.Add(new("Preview:failed:code", error, Quiet: true));
            return lines;
        }

        // The accept stops at the same command, so the preview says where.
        if (card.FailedIndex is { } failed)
            lines.Add(new("Preview:stop", $"Stops at command {failed + 1} of {proposal.Commands.Count}{(card.FailedCode is { } code ? $" ({code})" : string.Empty)}", Amber: true));

        string Name(Guid id) => card.BodyName(id) ?? (bodies.FirstOrDefault(body => body.Id == id) is { } live ? live.Name ?? live.Kind : id.ToString()[..8]);

        // The table "measure, before, after" (ADR-0187 item 6): the volume of each body that the commands add or change.
        if (card.Measures.Count == 0)
            lines.Add(new("Preview:measure:none", "No body changes", Quiet: true));
        else
        {
            lines.Add(new("Preview:header", "Measure", "Before", "After", Quiet: true));
            foreach (var (measure, index) in card.Measures.Take(PreviewRows).Select((measure, index) => (measure, index)))
            {
                var before = measure.Before is null ? "new" : Volume(measure.Before);
                var after = Volume(measure.After);
                lines.Add(new($"Preview:measure:{index}", Name(measure.BodyId), before, after, Amber: before != after));
            }

            if (card.Measures.Count > PreviewRows)
                lines.Add(new("Preview:measure:more", $"and {card.Measures.Count - PreviewRows} more", Quiet: true));
            // The atlas of the fonts has no glyph for a cube sign, so the unit is in words.
            lines.Add(new("Preview:unit", $"Measure: volume in cubic {unit}", Quiet: true));
        }

        if (card.Bodies is { Removed.Count: > 0 } removed)
            lines.Add(new("Preview:removed", removed.Removed.Count == 1 ? "1 body goes" : $"{removed.Removed.Count} bodies go", Quiet: true));

        // The state of the solver after the commands (ADR-0187 item 6), as the gap G12 of the render: "solved, 2 free".
        if (card.Sketches.Count > 0)
        {
            lines.Add(new("Preview:solver", "Solver after", Quiet: true));
            foreach (var (sketch, index) in card.Sketches.Take(PreviewRows).Select((sketch, index) => (sketch, index)))
            {
                var name = card.SketchNames.TryGetValue(sketch.SketchId, out var known) ? known : "New sketch";
                var after = sketch.After;
                var residual = after.State == SketchStates.Over && after.Residual is { } value ? $", residual {Number(value)}" : string.Empty;
                lines.Add(new($"Preview:solver:{index}", $"{name}: {after.State}, {after.Freedom} free{residual}", Amber: after.State == SketchStates.Over));
            }

            if (card.Sketches.Count > PreviewRows)
                lines.Add(new("Preview:solver:more", $"and {card.Sketches.Count - PreviewRows} more", Quiet: true));
        }

        // The checks that change state (ADR-0187 item 6). A body that does not exist counts as apart.
        if (card.Checks.Count > 0)
        {
            lines.Add(new("Preview:checks", "Checks that change", Quiet: true));
            foreach (var (pair, index) in card.Checks.Take(PreviewRows).Select((pair, index) => (pair, index)))
            {
                var (before, after) = (pair.Before?.State ?? PairStates.Apart, pair.After?.State ?? PairStates.Apart);
                lines.Add(new($"Preview:check:{index}", $"{Name(pair.BodyIdA)} · {Name(pair.BodyIdB)}: {before} {Arrow} {after}",
                    Amber: after is PairStates.Overlapping or PairStates.Limit or PairStates.Refused));
            }

            if (card.Checks.Count > PreviewRows)
                lines.Add(new("Preview:checks:more", $"and {card.Checks.Count - PreviewRows} more", Quiet: true));
        }

        // Item 2: a shape above the bound of the triangles is not drawn.
        if (card.ShapesLeftOut > 0)
            lines.Add(new("Preview:left-out", card.ShapesLeftOut == 1 ? "1 body is too large to draw" : $"{card.ShapesLeftOut} bodies are too large to draw", Quiet: true));
        return lines;
    }

    // A volume with groups of three digits, "320 000 000", or "no value" when the query refused the body.
    private static string Volume(BodyMeasure measure) => measure.Mass is { } mass ? Number(mass.Volume) : "no value";

    // A number with groups of three digits: with no decimal from 100, so the column of a table stays narrow, and with at most two
    // decimals below 100. Presentation (ADR-0007): the preview keeps the value to the double.
    internal static string Number(double value)
        => Math.Round(value, Math.Abs(value) >= 100 ? 0 : 2, MidpointRounding.AwayFromZero).ToString("#,0.##", CultureInfo.InvariantCulture).Replace(',', ' ');

    // Item 6: the least width of the panel.
    internal const float PreviewLeastWidth = 300f;

    // The state of the panel at the start of a frame: no panel until PreviewPanel shows one.
    private void BeginPreview()
    {
        _previewShown = false;
        PreviewRectangle = null;
        _previewTexts.Clear();
    }

    // Items 4 and 6: the panel of this frame, after the proposal.
    private void PreviewPanel(Vector2 displaySize, IReadOnlyList<LiveBody> bodies)
    {
        if (_proposals.Count == 0 || _preview is not { } card || card.Proposal != _proposals[0].Id || _portrait || PageOpen
            || displaySize.X < PreviewLeast.X || displaySize.Y < PreviewLeast.Y)
            return;

        var lines = PreviewLinesOf(card, _proposals[0], bodies, _unit).ToList();
        const float pad = 12f;
        const float size = ProposalTextSize;
        const float step = size + 6f;
        var width = CommandLineWidth(displaySize);
        var right = displaySize.X - 16f - _inset.Z;
        var bottom = (_items.TryGetValue(StatusLabel, out var status) ? status.Y : displaySize.Y - 16f - _inset.W - AnchorSize) - 6f;
        // The panel starts at least 8 pixels under the top bar and the command line, so the lines that do not fit go,
        // and the last line says how many.
        var least = 16f + _inset.Y + Math.Max(TopBarHeight, ImGui.GetFrameHeight()) + 8f;
        var fixedHeight = pad + step + MinimumControlHeight + 8f + pad;
        var room = Math.Max(1, (int)((bottom - least - fixedHeight) / step));
        if (lines.Count > room)
            lines = [.. lines.Take(room - 1), new PreviewLine("Preview:more", $"and {lines.Count - room + 1} more lines", Quiet: true)];
        var height = fixedHeight + (lines.Count * step);
        var top = bottom - height;
        // Item 6: the panel ends left of each target of a tool of the ring at its height when the room right of the targets holds
        // the least width; else it keeps its width and covers them, as the arc of a proposal keeps its place.
        var beside = (_ringTargets?.Values ?? Enumerable.Empty<Vector4>()).Where(target => target.Y < bottom && top < target.W && target.Z > right - width).ToList();
        if (beside.Count > 0 && right - (beside.Max(target => target.Z) + 8f) >= PreviewLeastWidth)
            width = right - (beside.Max(target => target.Z) + 8f);
        var left = right - width;

        ImGui.SetNextWindowPos(new Vector2(left, top), ImGuiCond.Always, Vector2.Zero);
        ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollWithMouse;
        if (ImGui.Begin($"{PreviewWindowName}###ProposalPreview", flags))
        {
            _previewShown = true;
            var list = ImGui.GetWindowDrawList();
            var font = ImGui.GetFont();
            var colours = ImGui.GetStyle().Colors;
            var (text, quiet) = (ImGui.GetColorU32(colours[(int)ImGuiCol.Text]), ImGui.GetColorU32(colours[(int)ImGuiCol.TextDisabled]));
            var inner = width - (2f * pad);
            var y = top + pad;

            void Put(string item, string shown, Vector2 at, uint colour)
            {
                ArrowText(list, font, size, at, colour, shown);
                _items[item] = new Vector4(at.X, at.Y, at.X + ArrowTextWidth(font, size, shown), at.Y + size);
                _previewTexts[item] = shown;
            }

            string FitTo(string shown, float room) => FitWith(font, size, shown, room);

            // The title, with "updating" while a later version waits for its preview.
            Put(PreviewTitleItem, FitTo(card.Current ? PreviewTitleText : $"{PreviewTitleText} · {PreviewUpdatingText}", inner), new Vector2(left + pad, y), DimensionColour);
            y += step;

            // Item 4: the switch, three segments of the height of a target (ADR-0027 item 6); the mode in use has the amber.
            var segment = inner / PreviewModes.Length;
            for (var i = 0; i < PreviewModes.Length; i++)
            {
                var (mode, label) = PreviewModes[i];
                var min = new Vector2(left + pad + (i * segment), y);
                var max = min + new Vector2(segment, MinimumControlHeight);
                ImGui.SetCursorScreenPos(min);
                if (ImGui.InvisibleButton(PreviewSwitchItem(mode), max - min))
                    _previewMode = mode;
                Item(PreviewSwitchItem(mode));
                var inUse = mode == _previewMode;
                if (inUse)
                    list.AddRectFilled(min, max, Faded(DimensionColour, 0.25f), 6f);
                else if (ImGui.IsItemHovered())
                    list.AddRectFilled(min, max, ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), 6f);
                list.AddRect(min, max, inUse ? DimensionColour : ImGui.GetColorU32(colours[(int)ImGuiCol.Border]), 6f, ImDrawFlags.None, inUse ? 2f : Theme.OutlineWidth);
                var labelSize = font.CalcTextSizeA(size, float.MaxValue, 0f, label);
                list.AddText(font, size, (min + max - labelSize) / 2f, inUse ? DimensionColour : text, label);
                _previewTexts[PreviewSwitchItem(mode)] = label;
            }

            y += MinimumControlHeight + 8f;

            // Item 6: the lines; a row of the table has three columns, the values at the right of their column.
            var (nameEnd, beforeEnd) = (left + pad + (inner * 0.42f), left + pad + (inner * 0.71f));
            foreach (var line in lines)
            {
                var colour = line.Quiet ? quiet : line.Amber && line.After is null ? DimensionColour : text;
                if (line.After is { } after)
                {
                    Put(line.Item, FitTo(line.Text, nameEnd - (left + pad) - 6f), new Vector2(left + pad, y), colour);
                    foreach (var (cell, end, key) in new[] { (line.Before ?? string.Empty, beforeEnd, ":before"), (after, left + pad + inner, ":after") })
                    {
                        var shown = FitTo(cell, (inner * 0.29f) - 6f);
                        var cellColour = line.Quiet ? quiet : key == ":after" && line.Amber ? DimensionColour : text;
                        Put(line.Item + key, shown, new Vector2(end - ArrowTextWidth(font, size, shown), y), cellColour);
                    }
                }
                else
                    Put(line.Item, FitTo(line.Text, inner), new Vector2(left + pad, y), colour);

                y += step;
            }

            AddPanelRectangle();
            PreviewRectangle = _panels[^1];
        }

        ImGui.End();
    }
}
