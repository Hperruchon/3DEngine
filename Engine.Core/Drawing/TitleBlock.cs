using Engine.Contracts.Drawing;
using Engine.Core.Export;
using Engine.Core.Queries;

namespace Engine.Core.Drawing;

// ADR-0179 items 4 to 6 (phase R30i, TASK-0428): the frame of a sheet, its centring marks, its title block with the fields of
// ISO 7200 and the symbol of the projection of ISO 5456-2, as lines and texts in millimetres of the paper. The SVG, the DXF and
// the PDF file and the sheet page draw this one list. The block is presentation: a read computes it, and no command, event or
// Document holds it. The code uses + - * / only, so the list is the same on each platform.
public static class TitleBlock
{
    // ADR-0179 item 4 (the answer Q6 A of the owner): the block is 180 mm wide, the largest width of ISO 7200, and as high as
    // its four rows of fields. Its lower right corner is the lower right corner of the frame.
    public const double Width = 180;
    public const double Height = 44;

    // ADR-0179 item 5: a centring mark goes from the edge of the paper to MarkInside past the frame (ISO 5457).
    public const double MarkInside = 5;

    // ADR-0179 item 4: the height of a capital letter of a label, of a value and of the title (ISO 3098), and the space between
    // a text and the lines of its cell.
    public const double LabelSize = 1.8;
    public const double ValueSize = 3.5;
    public const double TitleSize = 5;
    public const double Padding = 1.5;

    // ADR-0179 item 6: h of the symbol of the projection (ISO 5456-2): the large circle has the diameter 2h and the small circle
    // h; the cone is 2h long; the two views are h apart; a centre line goes Overrun past the symbol.
    public const double SymbolSize = 3.5;
    public const double Overrun = 1.5;

    // ADR-0179 item 4: one cell of the block: its field, its label, its box relative to the lower left corner of the block, and
    // the height of the capital letter of its value.
    public sealed record Cell(string Field, string Label, double X0, double Y0, double X1, double Y1, double Size);

    // ADR-0179 item 4: the cells, the lowest row first. The identification zone of ISO 7200 is the lowest row. "scale", "unit"
    // and "paper" come from the sheet; "projection" holds the symbol and no label.
    public static readonly IReadOnlyList<Cell> Cells =
    [
        new("legalOwner", "Legal owner", 0, 0, 55, 10, ValueSize),
        new("identificationNumber", "Identification number", 55, 0, 115, 10, ValueSize),
        new("revision", "Revision", 115, 0, 130, 10, ValueSize),
        new("issueDate", "Date of issue", 130, 0, 160, 10, ValueSize),
        new("sheetNumber", "Sheet", 160, 0, 180, 10, ValueSize),
        new("title", "Title", 0, 10, 125, 22, TitleSize),
        new("documentType", "Document type", 125, 10, 180, 22, ValueSize),
        new("creator", "Created by", 0, 22, 45, 32, ValueSize),
        new("approvalPerson", "Approved by", 45, 22, 90, 32, ValueSize),
        new("material", "Material", 90, 22, 180, 32, ValueSize),
        new("generalTolerances", "General tolerances", 0, 32, 90, 44, ValueSize),
        new("scale", "Scale", 90, 32, 112, 44, ValueSize),
        new("unit", "Unit", 112, 32, 130, 44, ValueSize),
        new("paper", "Paper", 130, 32, 145, 44, ValueSize),
        new("projection", "", 145, 32, 180, 44, ValueSize),
    ];

    // ADR-0179 item 4: the box of the block on the paper of the size, in millimetres: left, bottom, right, top.
    public static (double X0, double Y0, double X1, double Y1) Box(double width, double height)
        => (width - SvgExport.Margin - Width, SvgExport.Margin, width - SvgExport.Margin, SvgExport.Margin + Height);

    // ADR-0179 items 4 to 6: the lines and the texts of the frame, the centring marks, the block and the symbol of the sheet.
    public static TitleBlockLayout Layout(SheetInfo sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var (w, h) = (sheet.Width, sheet.Height);
        var (bx, by, _, _) = Box(w, h);
        var lines = new List<SheetLine>();
        void Line(double x0, double y0, double x1, double y1, string kind) => lines.Add(new SheetLine(Guid.Empty, x0, y0, x1, y1, true, kind));

        // ADR-0179 item 4: the frame, 10 mm inside the edge of the paper, thick.
        var (fx0, fy0, fx1, fy1) = (SvgExport.Margin, SvgExport.Margin, w - SvgExport.Margin, h - SvgExport.Margin);
        Line(fx0, fy0, fx1, fy0, "frame");
        Line(fx1, fy0, fx1, fy1, "frame");
        Line(fx1, fy1, fx0, fy1, "frame");
        Line(fx0, fy1, fx0, fy0, "frame");

        // ADR-0179 item 5: a centring mark at the middle of each side, thick, from the edge of the paper to 5 mm past the frame. The
        // mark of the lower side stops at the frame when the block holds the middle of the paper.
        var lower = bx <= w / 2 ? fy0 : fy0 + MarkInside;
        Line(w / 2, 0, w / 2, lower, "frame");
        Line(w / 2, h, w / 2, fy1 - MarkInside, "frame");
        Line(0, h / 2, fx0 + MarkInside, h / 2, "frame");
        Line(w, h / 2, fx1 - MarkInside, h / 2, "frame");

        // ADR-0179 item 4: the left side and the top of the block are thick; the lines between the rows and the cells are thin.
        Line(bx, by, bx, by + Height, "frame");
        Line(bx, by + Height, bx + Width, by + Height, "frame");
        foreach (var top in Cells.Select(cell => cell.Y1).Distinct().Where(top => top < Height))
            Line(bx, by + top, bx + Width, by + top, "block");
        foreach (var cell in Cells.Where(cell => cell.X1 < Width))
            Line(bx + cell.X1, by + cell.Y0, bx + cell.X1, by + cell.Y1, "block");

        // ADR-0179 item 4: the label of each cell at its upper left corner, and its value at its lower left corner. A text wider
        // than its cell gets a smaller size, so it stays inside the cell.
        var texts = new List<BlockText>();
        foreach (var cell in Cells)
        {
            var room = cell.X1 - cell.X0 - (2 * Padding);
            if (cell.Label.Length > 0)
                texts.Add(new BlockText(bx + cell.X0 + Padding, by + cell.Y1 - Padding - LabelSize, cell.Label, Fit(cell.Label, LabelSize, room)));
            var value = Value(sheet, cell.Field);
            if (value.Length > 0)
                texts.Add(new BlockText(bx + cell.X0 + Padding, by + cell.Y0 + 2, value, Fit(value, cell.Size, room)));
        }

        // ADR-0179 item 6: the symbol of the projection at the middle of its cell.
        var symbol = Cells.Single(cell => cell.Field == "projection");
        Symbol(lines, sheet.ThirdAngle, bx + ((symbol.X0 + symbol.X1) / 2), by + ((symbol.Y0 + symbol.Y1) / 2));
        return new TitleBlockLayout(lines, texts);
    }

    // ADR-0179 item 4: the text of a field. The title is the name of the sheet when the field is empty.
    public static string Value(SheetInfo sheet, string field)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var block = sheet.TitleBlock;
        return field switch
        {
            "title" => block.Title.Length > 0 ? block.Title : sheet.Name,
            "identificationNumber" => block.IdentificationNumber,
            "revision" => block.Revision,
            "issueDate" => block.IssueDate,
            "sheetNumber" => block.SheetNumber,
            "legalOwner" => block.LegalOwner,
            "material" => block.Material,
            "creator" => block.Creator,
            "generalTolerances" => block.GeneralTolerances,
            "approvalPerson" => block.ApprovalPerson,
            "documentType" => block.DocumentType,
            "scale" => SvgExport.Scale(sheet),
            "unit" => "mm",
            "paper" => sheet.Paper,
            _ => "",
        };
    }

    // ADR-0179 item 6: the symbol of ISO 5456-2: a cone seen from the side, a trapezium whose small end is on the left, and seen
    // from the left, two circles with one centre. In first angle the circles are on the right of the trapezium; in third angle
    // on its left. The outlines are thick, and the axis and the cross of the circles are centre lines.
    private static void Symbol(List<SheetLine> lines, bool thirdAngle, double cx, double cy)
    {
        const double s = SymbolSize;
        var x0 = cx - (2.5 * s);
        var (cone, circle) = thirdAngle ? (x0 + (3 * s), x0 + s) : (x0, x0 + (4 * s));
        void Line(double ax, double ay, double bx, double by, string kind) => lines.Add(new SheetLine(Guid.Empty, ax, ay, bx, by, true, kind));

        Line(cone, cy - (s / 2), cone, cy + (s / 2), "symbol");
        Line(cone + (2 * s), cy - s, cone + (2 * s), cy + s, "symbol");
        Line(cone, cy + (s / 2), cone + (2 * s), cy + s, "symbol");
        Line(cone, cy - (s / 2), cone + (2 * s), cy - s, "symbol");
        foreach (var radius in new[] { s, s / 2 })
        {
            lines.Add(new SheetLine(Guid.Empty, circle + radius, cy, circle + radius, cy, true, "symbol")
            {
                Circle = new SheetCircle(circle, cy, radius),
            });
        }

        Line(x0 - Overrun, cy, x0 + (5 * s) + Overrun, cy, "centre");
        Line(circle, cy - s - Overrun, circle, cy + s + Overrun, "centre");
    }

    // ADR-0179 item 4: the size of a text in its room: the size, or a smaller size at which the width of the text is the room.
    public static double Fit(string text, double size, double room)
    {
        ArgumentNullException.ThrowIfNull(text);
        var width = Measure(text, size);
        return width > room ? size * room / width : size;
    }

    // ADR-0179 item 4: the width of a text in millimetres, for a capital letter of the size: the widths of Helvetica in
    // thousandths of the size of its font, which is the size divided by the cap height (finding E224). A character with no width
    // in the table takes 1000, the width of a wide character, so the text stays inside its cell.
    public static double Measure(string text, double size)
    {
        ArgumentNullException.ThrowIfNull(text);
        double sum = 0;
        foreach (var ch in text)
        {
            sum += ch switch
            {
                >= 'A' and <= 'Z' => Capitals[ch - 'A'],
                >= 'a' and <= 'z' => Lowers[ch - 'a'],
                >= '0' and <= '9' => 556,
                ' ' or '.' or ',' or ':' or ';' or '/' or '!' => 278,
                '-' or '(' or ')' => 333,
                '+' or '=' or '±' => 584,
                '°' => 400,
                '_' or '#' => 556,
                '&' => 667,
                _ => 1000,
            };
        }

        return sum * SvgExport.FontSize(size) / 1000;
    }

    // The widths of the capital and the small letters of Helvetica, A to Z and a to z, from its metrics.
    private static readonly int[] Capitals = [667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611];
    private static readonly int[] Lowers = [556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500];
}

// ADR-0179 item 4: the lines of the frame, the marks, the block and the symbol, and the texts of the block. A line of the kind
// "frame" or "symbol" is thick, "block" thin, and "centre" a centre line (SvgExport.Stroke).
public sealed record TitleBlockLayout(IReadOnlyList<SheetLine> Lines, IReadOnlyList<BlockText> Texts);

// ADR-0179 item 4: a text of the block: X is its left end and Y its base line, in millimetres of the paper, and Size is the
// height of a capital letter in millimetres, as for SheetText.
public sealed record BlockText(double X, double Y, string Text, double Size);
