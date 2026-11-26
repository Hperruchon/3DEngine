using Engine.Contracts;

namespace Engine.Core.Queries;

// ADR-0189 item 1 (phase P21, TASK-0471): the live sheets of the Document. Before P21 an agent learned a sheet only from the
// event sheet.created while it listened (R-0075).
public sealed record GetSheetsQuery : Query
{
    public override string Name => "GetSheets";
    public override int SchemaVersion => 1;
}

// The result of GetSheets: each live sheet in the order of the Document (ADR-0189 item 3).
public sealed record SheetsInfo(IReadOnlyList<SheetEntry> Sheets);

// ADR-0189 item 2: one live sheet. The fields come from its record with no read of the design: the identifier that each
// command of a sheet and GetSheet take, the name, the paper, the orientation, the projection, and the identifier of each view
// in the order of the record. GetSheet gives the lines, the dimensions and the title block.
public sealed record SheetEntry(Guid SheetId, string Name, string Paper, bool Landscape, bool ThirdAngle, IReadOnlyList<Guid> ViewIds);
