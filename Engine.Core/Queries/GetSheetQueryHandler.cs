using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Drawing;

namespace Engine.Core.Queries;

// ADR-0155 item 5 (phase P10.2, TASK-0393). The query reads the Document and the owned kernel, so it needs no capability of
// the backend. ADR-0155 item 7 (Q7 A): no cache; each read computes the lines of each view.
public sealed class GetSheetQueryHandler : IQueryHandler
{
    public string QueryName => "GetSheet";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid"),
            ["name"] = new("string"),
            ["paper"] = new("string"),
            ["landscape"] = new("boolean"),
            ["thirdAngle"] = new("boolean"),
            ["width"] = new("number"),
            ["height"] = new("number"),
            ["views"] = new("array"),
            ["dimensions"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetSheetQuery
    {
        QueryId = input.QueryId,
        SheetId = (Guid)input.Parameters["sheetId"]!,
    };

    public Task<QueryHandlerResult> Handle(Query query, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var get = (GetSheetQuery)query;
        if (document.Sheets.FirstOrDefault(sheet => sheet.Id == get.SheetId) is not { } sheet)
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(DiagnosticCodes.SheetNotFound, $"No live sheet with id '{get.SheetId}' exists in the document.")));
        }

        var views = new List<SheetViewInfo>(sheet.Views.Count);
        foreach (var view in sheet.Views)
        {
            ct.ThrowIfCancellationRequested();
            var (lines, skipped) = SheetLayout.Place(document, view);
            views.Add(new SheetViewInfo(view.Id, view.Direction, view.ScaleNumerator, view.ScaleDenominator, view.X, view.Y, lines, skipped));
        }

        var (width, height) = SheetLayout.PaperSize(sheet);
        return Task.FromResult(QueryHandlerResult.Success(new SheetInfo(
            sheet.Id, sheet.Name, sheet.Paper, sheet.Landscape, sheet.ThirdAngle, width, height, views, sheet.Dimensions)));
    }
}
