using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Core;
using Engine.Core.Commands;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// ADR-0167 (phase R30b, TASK-0407): "Add to sheet", "Standard views" and the scale of the views, as commands of the session,
// the same commands that an agent sends, so the log records each one. Each method gives the text that the window shows.
internal sealed class SheetActions(DocumentSession session)
{
    // ADR-0167 item 2: the views of a flat view and of "Standard views" go on the first live sheet, at the scale of its first
    // view, each at the place of the projection. A view whose frame the sheet holds is not added again. With no live sheet the
    // window opens the pad of the paper and the scale first, so this method needs a sheet.
    // Finding V196 of the codebase review of 2026-11-18 (TASK-0410): the method runs on the queue of the sheet, so it reads the
    // sheet again first; an earlier work of the queue can have changed it.
    public string Add(SheetRecord sheet, IReadOnlyList<FlatView> views, SheetPlacement.Box box)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(views);
        if (Read(sheet.Id) is not { } live)
            return $"No live sheet with id '{sheet.Id}' exists.";
        sheet = live;
        var scale = sheet.Views.Count > 0
            ? (sheet.Views[0].ScaleNumerator, sheet.Views[0].ScaleDenominator)
            : SheetPlacement.FitScale(sheet.Paper, sheet.Landscape, sheet.ThirdAngle, box);
        var added = new List<string>();
        var held = new List<string>();
        var outside = new List<string>();
        foreach (var view in views)
        {
            var frame = SheetPlacement.FrameOf(view);
            if (sheet.Views.Any(existing => existing.Direction.SequenceEqual(frame)))
            {
                held.Add(Name(view));
                continue;
            }

            var (place, fits) = SheetPlacement.Place(sheet, frame, scale.Item1, scale.Item2, box);
            if (Apply(View(sheet.Id, frame, scale, place)) is { } refusal)
                return refusal;
            added.Add(Name(view));
            if (!fits)
                outside.Add(Name(view));
            sheet = Read(sheet.Id) ?? sheet;
        }

        return Text(sheet.Name, scale, added, held, outside);
    }

    // ADR-0167 items 3 and 5: the pad gives the paper and the scale; CreateSheet makes the sheet, and each view goes at its
    // place in the group of the views, centred on the area of the paper. Finding V194 of the codebase review of 2026-11-18
    // (TASK-0410): a view whose place is not inside the area does not go on the sheet, and the text names it; a refusal names
    // the views that went on before it. Until TASK-0410 the engine refused a place off the paper, and the text hid the views
    // that went on.
    public string Create(string name, string paper, bool landscape, (long Numerator, long Denominator) scale, IReadOnlyList<FlatView> views, SheetPlacement.Box box)
    {
        ArgumentNullException.ThrowIfNull(views);
        var create = new CreateSheetCommand { SheetName = name, Paper = paper, Landscape = landscape };
        if (Apply(create) is { } refusal)
            return refusal;
        var sheet = Read(create.CommandId) ?? throw new InvalidOperationException("The new sheet is not live.");

        // ADR-0167 item 3: a first view takes its place in the group of the standard views, so the next views fit beside it.
        IReadOnlyList<FlatView> group = [.. views.Concat(SheetPlacement.Standard.Where(view => !views.Contains(view)))];
        var places = SheetPlacement.Group(paper, landscape, sheet.ThirdAngle, [.. group.Select(SheetPlacement.FrameOf)], scale.Numerator, scale.Denominator, box);
        var added = new List<string>();
        var outside = new List<string>();
        for (var i = 0; i < views.Count; i++)
        {
            var frame = SheetPlacement.FrameOf(views[i]);
            if (!SheetPlacement.Fits(paper, landscape, frame, scale.Numerator, scale.Denominator, places[i], box))
            {
                outside.Add(Name(views[i]));
                continue;
            }

            if (Apply(View(sheet.Id, frame, scale, places[i])) is { } failed)
                return added.Count == 0 ? failed : $"{Text(name, scale, added, [], [])} {failed}";
            added.Add(Name(views[i]));
        }

        if (outside.Count == 0)
            return Text(name, scale, added, [], []);
        var made = added.Count == 0 ? $"Made {name} with no view." : Text(name, scale, added, [], []);
        return $"{made} {Capital(List(outside))} {(outside.Count == 1 ? "does" : "do")} not fit {paper} at {SheetPlacement.Text(scale)}: choose a smaller scale.";
    }

    // ADR-0167 item 5: ScaleSheetView for each view of the sheet, in one step of undo. The places stay.
    public string Scale(SheetRecord sheet, (long Numerator, long Denominator) scale)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        return Apply(new ScaleSheetViewCommand { SheetId = sheet.Id, ScaleNumerator = scale.Numerator, ScaleDenominator = scale.Denominator })
            ?? $"Each view of {sheet.Name} is at {SheetPlacement.Text(scale)}.";
    }

    // ADR-0168 item 3 (phase R30c, TASK-0408): a drag of the sheet page, one MoveSheetViews.
    public string Move(Guid sheet, Guid view, double x, double y, bool aligned)
        => Apply(new MoveSheetViewsCommand { SheetId = sheet, ViewId = view, X = x, Y = y, Aligned = aligned }) ?? (aligned ? "Moved the view and its aligned views." : "Moved the view.");

    // ADR-0168 item 2: "Remove" of the sheet page.
    public string Remove(Guid sheet, Guid view)
        => Apply(new RemoveSheetViewCommand { SheetId = sheet, ViewId = view }) ?? "Removed the view. Undo gives it back.";

    // ADR-0168 item 2: the switches of the hidden lines and the centre lines of a view, as SetSheetViewLines sets them.
    public string Lines(Guid sheet, Guid view, bool hidden, bool centre)
        => Apply(new SetSheetViewLinesCommand { SheetId = sheet, ViewId = view, HiddenLines = hidden, CentreLines = centre })
            ?? $"Hidden lines {(hidden ? "on" : "off")}, centre lines {(centre ? "on" : "off")}.";

    // ADR-0168 item 2: "Scale" of one view of the sheet page.
    public string ScaleView(Guid sheet, Guid view, (long Numerator, long Denominator) scale)
        => Apply(new ScaleSheetViewCommand { SheetId = sheet, ViewId = view, ScaleNumerator = scale.Numerator, ScaleDenominator = scale.Denominator })
            ?? $"The view is at {SheetPlacement.Text(scale)}.";

    // ADR-0168 item 5 (D5 B): a tap on the free paper places a view at that point, at the scale of the first view of the sheet,
    // or at the scale that fits for a sheet with no view.
    public string AddAt(Guid sheetId, FlatView view, double x, double y, SheetPlacement.Box? box)
    {
        if (Read(sheetId) is not { } sheet)
            return $"No live sheet with id '{sheetId}' exists.";
        var frame = SheetPlacement.FrameOf(view);
        if (sheet.Views.Any(existing => existing.Direction.SequenceEqual(frame)))
            return $"{sheet.Name} has the {Name(view)} view already.";
        var scale = sheet.Views.Count > 0
            ? (sheet.Views[0].ScaleNumerator, sheet.Views[0].ScaleDenominator)
            : box is { } b ? SheetPlacement.FitScale(sheet.Paper, sheet.Landscape, sheet.ThirdAngle, b) : (1L, 1L);
        return Apply(View(sheet.Id, frame, scale, (x, y))) ?? $"Added the {Name(view)} view to {sheet.Name} at {SheetPlacement.Text(scale)}.";
    }

    // ADR-0171 item 4 (TASK-0413): "Section" of the sheet page, one AddSheetSection with the first free letter.
    public string Section(Guid sheet, Guid view, string look, (double X, double Y, double Z) point, double x, double y)
    {
        var result = session.Apply(new AddSheetSectionCommand
        {
            SheetId = sheet, ViewId = view, Look = look, PointX = point.X, PointY = point.Y, PointZ = point.Z, X = x, Y = y,
        }).GetAwaiter().GetResult();
        if (result.Status != CommandStatus.Applied)
            return $"AddSheetSection: {result.Error?.Message}";
        return result.Outputs.TryGet<string>("letter", out var letter) ? $"Added the section {letter}-{letter}." : "Added the section.";
    }

    // The word of a flat view in a text, as "front".
    public static string Name(FlatView view) => view.ToString().ToLowerInvariant();

    private static AddSheetViewCommand View(Guid sheet, IReadOnlyList<double> d, (long Numerator, long Denominator) scale, (double X, double Y) place) => new()
    {
        SheetId = sheet, RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8],
        ScaleNumerator = scale.Numerator, ScaleDenominator = scale.Denominator, X = place.X, Y = place.Y,
    };

    // The text of an add: "Added the front and the top view to Sheet 1 at 2:1.", with the views that the sheet already holds and
    // the views that do not fit.
    private static string List(List<string> names) => names.Count == 1 ? $"the {names[0]} view" : $"the {string.Join(", the ", names.Take(names.Count - 1))} and the {names[^1]} view";

    private static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    private static string Text(string sheet, (long, long) scale, List<string> added, List<string> held, List<string> outside)
    {
        var parts = new List<string>();
        if (added.Count > 0)
            parts.Add($"Added {List(added)} to {sheet} at {SheetPlacement.Text(scale)}.");
        if (held.Count > 0)
            parts.Add($"{sheet} has {List(held)} already.");
        if (outside.Count > 0)
            parts.Add($"No free place fits {List(outside)}: move it on the sheet.");
        return string.Join(" ", parts);
    }

    private string? Apply(Command command)
    {
        var result = session.Apply(command).GetAwaiter().GetResult();
        return result.Status == CommandStatus.Applied ? null : $"{command.Name}: {result.Error?.Message}";
    }

    private SheetRecord? Read(Guid id)
        => session.Read((document, _) => document.Sheets.FirstOrDefault(sheet => sheet.Id == id)).GetAwaiter().GetResult();
}
