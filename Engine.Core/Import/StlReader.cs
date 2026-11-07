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
        // Finding E178 of the codebase review of 2026-11-07 (b) (TASK-0272): a short file with a zero byte is a cut binary
        // file, and a file whose first byte that is not text is in its first 80 bytes and is not zero is a text file with a
        // name that is not UTF-8, which the text reader refuses with its own message.
        if (StartsWithSolid(body))
        {
            var bad = FirstNonText(body);
            if (bytes.Length < HeaderBytes + 4 ? body.IndexOf((byte)0) < 0 : bad < 0 || (bad < HeaderBytes && body[bad] != 0))
                return Text(body);
        }

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

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var at = NameEnd(text, words);

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

    // The index of the first word after the name of the solid. ADR-0083 item 3: a recorded import reads its asset again at
    // each replay, so a change of the reader keeps the result of each file that an earlier reader accepted.
    //
    // Until TASK-0263 the name ended at the first word "facet" or "endsolid" (v0.243). Finding E170 (TASK-0263): a name may
    // hold those words, so the name became the rest of the line of "solid". Finding E173 of the codebase review of
    // 2026-11-07 (b) (TASK-0269): that rule lost a facet on the line of "solid" and refused a file of one line with a line
    // end, which v0.243 read. Now the name ends at the start of a whole facet, "facet normal", three numbers and "outer", on
    // any line, or at "facet" or "endsolid" after the line of "solid". Each file that v0.243 accepted has its first "facet"
    // there, so it gives the result of v0.243; a name on the line of "solid" may still hold "facet" and "endsolid".
    //
    // The review of Codex of v0.254 (TASK-0274): the rule of TASK-0269 ended the name at "facet normal" and one number, so it
    // refused a name such as "facet normal 0", which the reader of v0.250 to v0.253 read. A whole facet on the line of
    // "solid" had two earlier results: v0.243 read it, and v0.250 took it as the name; this rule gives the result of v0.243.
    private static int NameEnd(string text, string[] words)
    {
        var solid = text.Length - text.TrimStart().Length;
        var line = text.IndexOfAny(['\r', '\n'], solid);
        var onFirstLine = line < 0 ? words.Length : text[..line].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        bool Number(int at) => at < words.Length && float.TryParse(words[at], NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        for (var at = 1; at < words.Length; at++)
        {
            if (at >= onFirstLine && words[at] is "facet" or "endsolid")
                return at;
            if (words[at] == "facet" && at + 5 < words.Length && words[at + 1] == "normal"
                && Number(at + 2) && Number(at + 3) && Number(at + 4) && words[at + 5] == "outer")
                return at;
        }

        // No word ends the name: the rule of v0.243, so a file of one line with no facet gives E-STL-EMPTY as before.
        var end = 1;
        while (end < words.Length && words[end] != "facet" && words[end] != "endsolid")
            end++;
        return end;
    }

    // The byte order mark of UTF-8, which some programs write at the start of a text file (E170).
    private static ReadOnlySpan<byte> ByteOrderMark => [0xEF, 0xBB, 0xBF];

    // Text is UTF-8 with no zero byte; the header and the floats of a binary file hold zero bytes or bytes that are not
    // UTF-8 in nearly each file.
    private static bool IsText(ReadOnlySpan<byte> bytes)
        => bytes.IndexOf((byte)0) < 0 && System.Text.Unicode.Utf8.IsValid(bytes);

    // E178 (TASK-0272): the index of the first byte that is zero or that starts no valid UTF-8 character, or -1.
    private static int FirstNonText(ReadOnlySpan<byte> bytes)
    {
        var at = 0;
        while (at < bytes.Length)
        {
            if (bytes[at] == 0 || Rune.DecodeFromUtf8(bytes[at..], out _, out var length) != System.Buffers.OperationStatus.Done)
                return at;
            at += length;
        }

        return -1;
    }

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
