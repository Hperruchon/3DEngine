using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Input;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0098 (the redesign, slice o, TASK-0312): the command line of the render "mix", at the upper right of a landscape
// display. Control and K give it the keys. A text lists the commands of this state whose name holds it; Enter, or a tap,
// runs the first one or the one that the arrows chose, as its button or its segment does. Escape empties it and gives
// the keys back to the scene. A command that needs a number opens its pad (ADR-0032), so each value of the model still
// goes through the pad and its evaluator.
internal sealed unsafe partial class DesktopUi
{
    // The item of the field, the item of a listed command, and the text of the empty field.
    public const string CommandItem = "Command";
    // ADR-0108 item 4 (phase R18a, TASK-0332): the text of the render. A typed value is slice S8 (R18h); until then a number
    // finds no command. Until TASK-0332 the text was "Type a command".
    public const string CommandHint = "Type a command or a value";

    // ADR-0108 item 4: the text of a field too narrow for the text of the render, so no letter is cut.
    public const string CommandHintShort = "Type a command";

    // ADR-0108 item 4: the text of the empty field that fits in this width.
    private static string CommandHintFor(float width)
        => (2f * CommandTextLeft) + ImGui.CalcTextSize(CommandHint).X <= width ? CommandHint : CommandHintShort;

    // ADR-0108 item 4: the item of the keys that open the field, and their text. The input has no key of the Command key of
    // macOS yet (register entry R-0060), so each platform shows "Ctrl K".
    public const string CommandKeyItem = "Command:key";
    public const string CommandKeyText = "Ctrl K";

    // ADR-0108 item 4: the left room of the text of the field, which holds the search mark.
    private const float CommandTextLeft = 34f;

    // ADR-0108 item 4: the search mark at the left of the field, and the keys at its right while it is empty.
    private void CommandMarks(bool empty, string shown)
    {
        var (min, max) = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        var list = ImGui.GetWindowDrawList();
        // The field fills its window, whose clip cuts an outline on its edge; so the outline is 1 pixel inside the field.
        var hovered = ImGui.IsItemHovered() || ImGui.IsItemActive();
        list.AddRect(min + Vector2.One, max - Vector2.One, ImGui.GetColorU32(hovered ? Theme.Vector(0x30EAFFFF) : ImGui.GetStyle().Colors[(int)ImGuiCol.Border]), ((max.Y - min.Y) / 2f) - 1f, ImDrawFlags.None, Theme.OutlineWidth);
        var lens = new Vector2(min.X + 16f, ((min.Y + max.Y) / 2f) - 1.5f);
        list.AddCircle(lens, 5f, Theme.Accent, 0, 1.5f);
        list.AddLine(lens + new Vector2(3.6f, 3.6f), lens + new Vector2(7.5f, 7.5f), Theme.Accent, 1.5f);
        var hint = min.X + CommandTextLeft + ImGui.CalcTextSize(shown).X;
        ImGui.PushFont(_monoFont);
        var size = ImGui.CalcTextSize(CommandKeyText);
        ImGui.PopFont();
        var at = new Vector2(max.X - 18f - size.X, ((min.Y + max.Y) / 2f) - (size.Y / 2f));
        // The keys show on an empty field that has room for them right of the text, so they never cover a letter.
        if (!empty || at.X - 8f < hint)
            return;
        ImGui.PushFont(_monoFont);
        list.AddText(at, ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]), CommandKeyText);
        ImGui.PopFont();
        _items[CommandKeyItem] = new Vector4(at.X, at.Y, at.X + size.X, at.Y + size.Y);
    }

    public static string CommandListItem(string name) => $"Command:{name}";

    // Item 2: the width of the field, and the count of commands that the list shows at most. ADR-0108 item 4 (phase R18a,
    // TASK-0332): 400 pixels, so the search mark, the text of the render and "Ctrl K" fit in the font of the window. Until
    // TASK-0332 it was 300 pixels.
    internal const float CommandWidth = 400f;
    internal const int CommandListLength = 8;

    // ADR-0113 item 4 (phase R19, TASK-0336): on a narrow landscape display the field is 30 percent of the width, at least 200
    // pixels, so the top bar and the panel keep their room at its left. Until TASK-0336 it was 300 pixels at each width, and
    // at 640 by 360 it overlapped the top bar and the panel.
    internal static float CommandLineWidth(Vector2 display) => Math.Min(CommandWidth, Math.Max(display.X * 0.3f, 200f));

    // ADR-0113 item 4: the left side of the field on a landscape display.
    private float CommandLeft(Vector2 display) => display.X - 16f - _inset.Z - CommandLineWidth(display);

    private string _command = string.Empty;
    private int _commandIndex;
    private bool _focusCommand;

    // Item 1: Control and K, down, give the field the keys in the next frame.
    private void WatchCommandKey(IReadOnlyList<InputSample> samples)
    {
        if (samples.Any(sample => sample is KeySample { Key: InputKey.K, Down: true } key && (key.Modifiers & KeyModifiers.Control) != 0))
            _focusCommand = true;
    }

    // Item 3: the commands of this state, in the order of the list: the commands of the active sketch, or of the document
    // and of the selected body.
    internal IReadOnlyList<(string Name, Action Run)> Commands(IReadOnlyList<LiveBody> bodies, Guid? selection)
    {
        var commands = new List<(string Name, Action Run)>();
        if (_sketchActive)
        {
            foreach (var (label, _) in SketchRingTools)
                commands.Add((label, () => UseSketchTool(label)));
            commands.Add((NextPointLabel, () => OpenNextPointPad(_sketchPanel?.ChainOpen ?? false)));
        }
        else
        {
            foreach (var label in new[] { NewBoxLabel, NewCylinderLabel, NewConeLabel, NewSphereLabel, NewTorusLabel })
                commands.Add((label, () => OpenNew(label)));
            // ADR-0102 item 1 (phase P9g, TASK-0316): the controls of a file, because the rows of a selected body hide the
            // panel (ADR-0099); a host with no files has none.
            if (_files)
            {
                commands.Add((OpenProjectLabel, () => _actions.Add(new OpenProjectAction())));
                commands.Add((SaveAsLabel, () => _actions.Add(new SaveAsAction())));
                commands.Add((ExportStlLabel, () => _actions.Add(new ExportStlAction())));
            }

            commands.Add((ImportStlLabel, () => _actions.Add(new ImportStlAction())));
            foreach (var (label, datum) in NewSketchButtons)
                commands.Add((label, () => _actions.Add(new NewSketchAction(datum))));
            // ADR-0099 item 4 (the redesign, slice p, TASK-0313): the controls of the panel that the rows of a selected body
            // hide: the armed tap on a face, the toggles of the dimensions, and the name field of the selected body.
            commands.Add((SketchOnFaceLabel, () => _actions.Add(new SketchOnFaceAction())));
            commands.Add((DimensionsLabel, () => _dimensions = !_dimensions));
            if (_dimensions)
                commands.Add((HiddenTooLabel, () => _hiddenToo = !_hiddenToo));
            if (bodies.FirstOrDefault(body => body.Id == selection) is { } body)
            {
                commands.Add((RenameLabel, () => StartRename(body)));
                // Finding V153 of the codebase review of 2026-11-09 (TASK-0324): the name of a cut holds the text of the row of
                // its tool, as the list of a cut in the rows does, so two bodies with no name and of one kind give two names.
                // Until TASK-0324 the name held the kind only, and the list showed "Cut with Box" two times.
                foreach (var tool in bodies.Where(other => other.Id != body.Id && other.Visible))
                    commands.Add(($"{CutLabel} {RowText(tool)}", () => _actions.Add(new CutBodyAction(body.Id, tool.Id))));
                commands.Add((MoveLabel, () => Use(MoveLabel, body)));
                if (body.Kind == "Extrude")
                    commands.Add((SetDistanceLabel, () => Use(SetDistanceLabel, body)));
                var visible = body.Visible ? HideLabel : ShowLabel;
                commands.Add((visible, () => Use(visible, body)));
            }

            commands.Add((ShowAllLabel, () => _actions.Add(new ShowAllAction())));
        }

        commands.Add((UndoLabel, () => _actions.Add(new UndoAction())));
        commands.Add((RedoLabel, () => _actions.Add(new RedoAction())));
        commands.Add((FrameLabel, () => _actions.Add(new FrameAction())));
        return commands;
    }

    // Item 3: the commands whose name holds the text, with no regard to the case; a name that starts with it comes first.
    internal static IReadOnlyList<(string Name, Action Run)> Matches(IReadOnlyList<(string Name, Action Run)> commands, string text)
    {
        var query = text.Trim();
        if (query.Length == 0)
            return [];
        return [.. commands
            .Select((command, order) => (command, order, at: command.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase)))
            .Where(match => match.at >= 0)
            .OrderBy(match => match.at == 0 ? 0 : 1)
            .ThenBy(match => match.order)
            .Select(match => match.command)
            .Take(CommandListLength)];
    }

    // Items 1 to 4: the field at the upper right of a landscape display, and the list of the matches under it.
    private void CommandLine(Vector2 displaySize, IReadOnlyList<LiveBody> bodies, Guid? selection)
    {
        // Finding V157 of the codebase review of 2026-11-09 (TASK-0324): a field that does not show takes no request of the
        // focus, so Control and K on a portrait display does not give the field the keys later. Until TASK-0324 the request
        // stayed, and the field took the keys after a turn of the display.
        if (displaySize.X < displaySize.Y)
        {
            _focusCommand = false;
            return;
        }

        var right = displaySize.X - 16f - _inset.Z;
        var width = Math.Min(CommandLineWidth(displaySize), Math.Max(right - 16f - _inset.X, 0f));
        ImGui.SetNextWindowPos(new Vector2(right - width, 16f + _inset.Y), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("##Command", FixedFlags))
        {
            if (_focusCommand)
            {
                ImGui.SetKeyboardFocusHere();
                _focusCommand = false;
            }

            // ADR-0108 item 4 (phase R18a, TASK-0332): a round outlined field, with room for the search mark at its left.
            ImGui.SetNextItemWidth(width);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, ImGui.GetFrameHeight() / 2f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(CommandTextLeft, ImGui.GetStyle().FramePadding.Y));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
            var shown = CommandHintFor(width);
            var enter = ImGui.InputTextWithHint("##CommandText", shown, ref _command, 128, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.PopStyleVar(3);
            var active = ImGui.IsItemActive();
            var left = ImGui.IsItemDeactivated();
            Item(CommandItem);
            CommandMarks(_command.Length == 0, shown);

            var matches = Matches(Commands(bodies, selection), _command);
            if (active && ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                _commandIndex++;
            if (active && ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                _commandIndex--;
            _commandIndex = matches.Count == 0 ? 0 : Math.Clamp(_commandIndex, 0, matches.Count - 1);

            (string Name, Action Run)? chosen = enter && matches.Count > 0 ? matches[_commandIndex] : null;
            for (var index = 0; index < matches.Count; index++)
            {
                // Finding V153 (TASK-0324): each listed command has an identifier of its own, also when two names are equal.
                // Until TASK-0324 two equal names shared one identifier, and a tap on the second one gave no action.
                if (ImGui.Selectable($"{matches[index].Name}##Command{index}", index == _commandIndex, ImGuiSelectableFlags.None, new Vector2(width, MinimumControlHeight)))
                    chosen = matches[index];
                Item(CommandListItem(matches[index].Name));
            }

            if (chosen is { } run)
            {
                run.Run();
                (_command, _commandIndex, _dropFocus) = (string.Empty, 0, true);
            }
            else if ((active || left) && ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                (_command, _commandIndex, _dropFocus) = (string.Empty, 0, true);
            }

            AddWindowRectangle();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }
}
