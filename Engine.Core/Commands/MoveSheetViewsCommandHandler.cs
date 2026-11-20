using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0168 item 3 (phase R30c, TASK-0408): a new record of the sheet with the view at its new place and, with Aligned, each
// view on a line of projection through it moved by the same distance. The checks come in the order of the record: the sheet
// is live, the view is on it, and each new place is on the paper. The handler needs no capability of the backend.
public sealed class MoveSheetViewsCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "MoveSheetViews";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
            ["viewId"] = new("guid", Required: true),
            ["x"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["y"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["aligned"] = new("boolean", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["viewId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (MoveSheetViewsCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sheetId"] = typed.SheetId,
            ["viewId"] = typed.ViewId,
            ["x"] = typed.X,
            ["y"] = typed.Y,
            ["aligned"] = typed.Aligned,
        };
    }

    public Command Create(CommandInput input) => new MoveSheetViewsCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        ViewId = (Guid)input.Parameters["viewId"]!,
        X = (double)input.Parameters["x"]!,
        Y = (double)input.Parameters["y"]!,
        Aligned = input.Parameters.TryGetValue("aligned", out var aligned) && aligned is true,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var move = (MoveSheetViewsCommand)command;
        if (SheetResults.Find(document, move.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(move.SheetId));
        if (sheet.Views.FirstOrDefault(view => view.Id == move.ViewId) is not { } moved)
            return Task.FromResult(SheetResults.ViewNotFound(sheet.Id, move.ViewId));
        // Finding E225 of the codebase review of 2026-11-18 (b) (TASK-0416): a move to the place that the view has changes
        // nothing, so it is refused and gives no step of undo. Until TASK-0416 it was Applied.
        if (move.X == moved.X && move.Y == moved.Y)
            return Task.FromResult(SheetResults.ViewInvalid(
                $"The view '{moved.Id}' is at ({moved.X}, {moved.Y}) already, so the move changes nothing (ADR-0168 item 3)."));

        var (dx, dy) = (move.X - moved.X, move.Y - moved.Y);
        var views = new List<SheetView>(sheet.Views.Count);
        foreach (var view in sheet.Views)
        {
            var next = view.Id == moved.Id
                ? view with { X = move.X, Y = move.Y }
                : move.Aligned && OnLine(moved, view) ? view with { X = view.X + dx, Y = view.Y + dy } : view;
            if (next != view && SheetResults.PlaceRefused(sheet, next.X, next.Y) is { } refused)
                return Task.FromResult(refused);
            views.Add(next);
        }

        var outputs = new Outputs(new Dictionary<string, object?> { ["viewId"] = move.ViewId });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [sheet with { Views = views }]));
    }

    // ADR-0168 item 3: a view on a line of projection through the moved view: the same right and the same X, or the same up and
    // the same Y. The numbers compare with no tolerance, as the places of the window keep them.
    internal static bool OnLine(SheetView moved, SheetView view)
    {
        static bool Same(IReadOnlyList<double> a, IReadOnlyList<double> b, int first) => a[first] == b[first] && a[first + 1] == b[first + 1] && a[first + 2] == b[first + 2];
        return (Same(moved.Direction, view.Direction, 0) && moved.X == view.X) || (Same(moved.Direction, view.Direction, 3) && moved.Y == view.Y);
    }
}
