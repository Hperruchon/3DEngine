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
    public const string CommandHint = "Type a command";

    public static string CommandListItem(string name) => $"Command:{name}";

    // Item 2: the width of the field, and the count of commands that the list shows at most.
    internal const float CommandWidth = 300f;
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

            ImGui.SetNextItemWidth(width);
            var enter = ImGui.InputTextWithHint("##CommandText", CommandHint, ref _command, 128, ImGuiInputTextFlags.EnterReturnsTrue);
            var active = ImGui.IsItemActive();
            var left = ImGui.IsItemDeactivated();
            Item(CommandItem);

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
