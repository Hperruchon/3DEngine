using Engine.Contracts;
using Engine.Core;
using Engine.Core.Export;
using Engine.Core.Queries;

namespace ThreeDEngine.Desktop;

// ADR-0170 item 1 (phase R30d, TASK-0412): the three files of a sheet, PDF first.
internal enum SheetFormat
{
    Pdf,
    Svg,
    Dxf,
}

// ADR-0170 (phase R30d, TASK-0412): the export of a sheet from the window. The window asks the save dialog of the system for a
// path, reads the file of the sheet from the Document with the code of the HTTP route and of the command line of the engine,
// and writes it through SDL off the frame thread. The file of the project does not change.
internal static class SheetExports
{
    // ADR-0170 item 1: the formats in the order of the page: PDF, SVG and DXF.
    public static readonly IReadOnlyList<SheetFormat> Formats = [SheetFormat.Pdf, SheetFormat.Svg, SheetFormat.Dxf];

    // The word of a format on a button: "PDF", "SVG" or "DXF".
    public static string Label(SheetFormat format) => format.ToString().ToUpperInvariant();

    // The filter of the save dialog of a format.
    public static (string Name, string Pattern) Filter(SheetFormat format) => format switch
    {
        SheetFormat.Pdf => ("PDF file", "pdf"),
        SheetFormat.Svg => ("SVG file", "svg"),
        SheetFormat.Dxf => ("DXF file", "dxf"),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "A format of a sheet is PDF, SVG or DXF."),
    };

    // ADR-0170 item 2: the purpose of the save dialog of each format, so a late result of another dialog never writes a sheet.
    public static DialogPurpose Purpose(SheetFormat format) => format switch
    {
        SheetFormat.Pdf => DialogPurpose.ExportPdf,
        SheetFormat.Svg => DialogPurpose.ExportSvg,
        SheetFormat.Dxf => DialogPurpose.ExportDxf,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "A format of a sheet is PDF, SVG or DXF."),
    };

    public static SheetFormat? FormatOf(DialogPurpose purpose) => purpose switch
    {
        DialogPurpose.ExportPdf => SheetFormat.Pdf,
        DialogPurpose.ExportSvg => SheetFormat.Svg,
        DialogPurpose.ExportDxf => SheetFormat.Dxf,
        _ => null,
    };

    // ADR-0170 item 2: the save dialog starts at "<project> <sheet>.<format>" beside the file of the project, or where the system
    // chooses for a document with no file. A character that a file name does not permit becomes "_".
    public static string? Location(string? project, string sheet, SheetFormat format)
    {
        if (project is null || Path.GetDirectoryName(project) is not { } folder)
            return null;
        var name = $"{Path.GetFileNameWithoutExtension(project)} {sheet}";
        var invalid = Path.GetInvalidFileNameChars().Concat(['/', '\\', ':', '*', '?', '"', '<', '>', '|']).ToHashSet();
        name = new string([.. name.Select(ch => invalid.Contains(ch) || char.IsControl(ch) ? '_' : ch)]);
        return Path.Combine(folder, $"{name}.{Filter(format).Pattern}");
    }

    // ADR-0170 item 3: the file of the sheet at the path, with the extension of the format when the dialog left it out; a content
    // URI of Android keeps its name. Save writes the bytes and gives null, or the reason of a failure. The text says what
    // happened, with each body that gives no lines.
    public static string Write(DocumentSession session, Guid sheet, SheetFormat format, string path, Func<string, byte[], string?> save)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(save);
        var (content, skipped, error, name) = session.Read((document, _) =>
        {
            var file = format switch
            {
                SheetFormat.Pdf => PdfExport.Write(document, sheet),
                SheetFormat.Svg => SvgExport.Write(document, sheet),
                _ => DxfExport.Write(document, sheet),
            };
            return (file.Content, file.Skipped, file.Error, document.Sheets.FirstOrDefault(live => live.Id == sheet)?.Name);
        }).GetAwaiter().GetResult();
        if (error is not null)
            return $"{error.Code}: {error.Message}";

        var target = path.StartsWith("content://", StringComparison.OrdinalIgnoreCase)
            ? path
            : ProjectFiles.WithExtension(path, "." + Filter(format).Pattern);
        if (save(target, content!) is { } failure)
            return $"{DiagnosticCodes.ExportWriteFailed}: {failure}";
        var text = $"Exported {name} as {Label(format)} to {Path.GetFileName(target)}, {content!.Length} bytes.";
        return skipped.Count == 0 ? text : $"{text} {string.Join(" ", skipped.Select(body => $"The body {body.Body} gives no lines: {body.Reason}"))}";
    }
}
