using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Engine.Contracts;

namespace Engine.Core.Import;

// ADR-0083 item 3 (phase P9b, TASK-0258): the reader of an STL file, binary and text. It gives the triangles as 32-bit
// floats, nine for each triangle (x, y and z of its three vertices), in the order of the file, or one code. It does no
// arithmetic on a coordinate: a binary file gives the bits of each float as they are, and a text file gives each number
// as the nearest 32-bit float, which .NET gives in the same way on each platform. The normal of a triangle is not read.
public static class StlReader
{
    // ADR-0083 item 6: the limit of triangles of one file.
    public const int MaximumTriangles = 300_000;

    private const int HeaderBytes = 80;
    private const int TriangleBytes = 50;

    public static (float[]? Triangles, ErrorDetail? Error) Read(ReadOnlySpan<byte> bytes)
    {
        // A file whose length is 84 plus 50 times its count is binary, even when its header starts with "solid", as the
        // header of some programs does.
        if (bytes.Length >= HeaderBytes + 4)
        {
            var count = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(HeaderBytes, 4));
            if (bytes.Length == HeaderBytes + 4 + ((long)count * TriangleBytes))
                return Binary(bytes, count);
        }

        // Finding E170 of the codebase review of 2026-11-07 (TASK-0263): a file of at least 84 bytes that starts with
        // "solid" and is not text is a binary file of the wrong length, so it gives E-STL-TRUNCATED, not E-STL-SYNTAX.
        var body = bytes.StartsWith(ByteOrderMark) ? bytes[ByteOrderMark.Length..] : bytes;
        if (StartsWithSolid(body) && (bytes.Length < HeaderBytes + 4 || IsText(body)))
            return Text(body);

        return (null, Error(DiagnosticCodes.StlTruncated, bytes.Length >= HeaderBytes + 4
            ? $"The file has {bytes.Length} bytes, and a binary STL file with the count of its header has {HeaderBytes + 4 + ((long)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(HeaderBytes, 4)) * TriangleBytes)}. The file is cut or is not an STL file."
            : $"The file has {bytes.Length} bytes: it is not a text STL file, and a binary STL file has at least {HeaderBytes + 4}."));
    }

    private static (float[]?, ErrorDetail?) Binary(ReadOnlySpan<byte> bytes, uint count)
    {
        if (count == 0)
            return (null, Error(DiagnosticCodes.StlEmpty, "The binary STL file holds no triangle."));
        if (count > MaximumTriangles)
            return (null, TooLarge(count));

        var triangles = new float[count * 9];
        for (var t = 0; t < count; t++)
        {
            // Each triangle: a normal of three floats, which is not read, then three vertices of three floats.
            var start = HeaderBytes + 4 + (t * TriangleBytes) + 12;
            for (var i = 0; i < 9; i++)
            {
                var value = BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(start + (i * 4), 4));
                if (!float.IsFinite(value))
                    return (null, NotFinite(t));
                triangles[(t * 9) + i] = value;
            }
        }

        return (triangles, null);
    }

    private static (float[]?, ErrorDetail?) Text(ReadOnlySpan<byte> bytes)
    {
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return (null, Error(DiagnosticCodes.StlSyntax, "The text STL file is not UTF-8 or ASCII text."));
        }

        // The name of the solid is the rest of the line of "solid" (E170), so a name may hold "facet" or "endsolid". A file
        // of one line has no end of that line, so its name ends at the first facet or "endsolid".
        var solid = text.Length - text.TrimStart().Length;
        var line = text.IndexOfAny(['\r', '\n'], solid);
        var words = (line < 0 ? text : text[line..]).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var at = line < 0 ? 1 : 0;
        while (line < 0 && at < words.Length && words[at] != "facet" && words[at] != "endsolid")
            at++;

        var triangles = new List<float>();
        while (true)
        {
            if (at >= words.Length)
                return (null, Error(DiagnosticCodes.StlSyntax, "The text STL file ends before \"endsolid\"."));
            if (words[at] == "endsolid")
                break;

            var triangle = triangles.Count / 9;
            if (!Expect(words, ref at, "facet") || !Expect(words, ref at, "normal") || !Numbers(words, ref at, 3, null)
                || !Expect(words, ref at, "outer") || !Expect(words, ref at, "loop"))
                return (null, Syntax(words, at, triangle));
            for (var v = 0; v < 3; v++)
            {
                if (!Expect(words, ref at, "vertex"))
                    return (null, Syntax(words, at, triangle));
                if (!Numbers(words, ref at, 3, triangles))
                    return (null, Syntax(words, at, triangle));
            }

            if (!Expect(words, ref at, "endloop") || !Expect(words, ref at, "endfacet"))
                return (null, Syntax(words, at, triangle));
            if (triangles.Skip(triangles.Count - 9).Any(value => !float.IsFinite(value)))
                return (null, NotFinite(triangle));
            if (triangles.Count / 9 > MaximumTriangles)
                return (null, TooLarge(triangles.Count / 9));
        }

        if (triangles.Count == 0)
            return (null, Error(DiagnosticCodes.StlEmpty, "The text STL file holds no triangle."));
        return ([.. triangles], null);
    }

    // The byte order mark of UTF-8, which some programs write at the start of a text file (E170).
    private static ReadOnlySpan<byte> ByteOrderMark => [0xEF, 0xBB, 0xBF];

    // Text is UTF-8 with no zero byte; the header and the floats of a binary file hold zero bytes or bytes that are not
    // UTF-8 in nearly each file.
    private static bool IsText(ReadOnlySpan<byte> bytes)
        => bytes.IndexOf((byte)0) < 0 && System.Text.Unicode.Utf8.IsValid(bytes);

    private static bool StartsWithSolid(ReadOnlySpan<byte> bytes)
    {
        var at = 0;
        while (at < bytes.Length && bytes[at] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            at++;
        return bytes[at..].StartsWith("solid"u8);
    }

    private static bool Expect(string[] words, ref int at, string word)
    {
        if (at >= words.Length || words[at] != word)
            return false;
        at++;
        return true;
    }

    // Three numbers; the vertices go into the list, and the normal (list null) is checked as a number only.
    private static bool Numbers(string[] words, ref int at, int count, List<float>? into)
    {
        for (var i = 0; i < count; i++)
        {
            if (at >= words.Length || !float.TryParse(words[at], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return false;
            into?.Add(value);
            at++;
        }

        return true;
    }

    private static ErrorDetail Syntax(string[] words, int at, int triangle)
        => Error(DiagnosticCodes.StlSyntax, at >= words.Length
            ? $"The text STL file ends inside triangle {triangle + 1}, before \"endsolid\"."
            : $"The text STL file has the word \"{words[at]}\" in triangle {triangle + 1}, where STL has another word or a number.");

    private static ErrorDetail NotFinite(int triangle)
        => Error(DiagnosticCodes.StlNotFinite, $"Triangle {triangle + 1} of the STL file has a coordinate that is not a finite number.");

    private static ErrorDetail TooLarge(long count)
        => Error(DiagnosticCodes.StlTooLarge, $"The STL file has {count} triangles, and the engine opens at most {MaximumTriangles} (ADR-0083 item 6).");

    private static ErrorDetail Error(string code, string message) => new(code, message);
}
