using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Queries;

// ADR-0189 items 1 to 3 (phase P21, TASK-0471). The query reads the records of the sheets in the Document, so it needs no
// capability of the backend, gives no error and writes nothing to the log.
public sealed class GetSheetsQueryHandler : IQueryHandler
{
    public string QueryName => "GetSheets";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheets"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetSheetsQuery { QueryId = input.QueryId };

    public Task<QueryHandlerResult> Handle(Query query, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var live = document.Sheets.ToDictionary(sheet => sheet.Id);
        var sheets = InLogOrder(document, live)
            .Select(sheet => new SheetEntry(sheet.Id, sheet.Name, sheet.Paper, sheet.Landscape, sheet.ThirdAngle, [.. sheet.Views.Select(view => view.Id)]))
            .ToList();
        return Task.FromResult(QueryHandlerResult.Success(new SheetsInfo(sheets)));
    }

    // Item 3: the live records in the order of the log of the command that made each one. The identifier of a body is the
    // identifier of its command (ADR-0012 §4), CreateSheet gives a sheet the identifier of its command, and the log holds each
    // command once.
    internal static IEnumerable<T> InLogOrder<T>(Document document, IReadOnlyDictionary<Guid, T> live)
    {
        foreach (var entry in document.Log)
        {
            if (live.TryGetValue(entry.Command.CommandId, out var record))
                yield return record;
        }
    }
}
