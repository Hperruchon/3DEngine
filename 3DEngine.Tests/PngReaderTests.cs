using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ThreeDEngine.Core.Imaging;

namespace ThreeDEngine.Tests;

// ADR-0046 item 1 (TASK-0153): the reader of PNG files gives 8-bit RGBA, the top row first, for
// each colour type and each filter, and refuses each form that it does not read with a reason.
public class PngReaderTests
{
    // Each colour type, 2 by 1 pixels: the left pixel, then the right one.
    [Fact]
    public void Each_Colour_Type_Gives_Its_Rgba()
    {
        var grey = PngReader.Read(Encode(2, 1, 0, [[10, 200]]));
        var rgb = PngReader.Read(Encode(2, 1, 2, [[1, 2, 3, 4, 5, 6]]));
        var palette = PngReader.Read(Encode(2, 1, 3, [[1, 0]], palette: [9, 8, 7, 60, 70, 80]));
        var greyAlpha = PngReader.Read(Encode(2, 1, 4, [[50, 128, 51, 0]]));
        var rgba = PngReader.Read(Encode(2, 1, 6, [[1, 2, 3, 4, 5, 6, 7, 8]]));

        Assert.Equal([10, 10, 10, 255, 200, 200, 200, 255], grey.Rgba);
        Assert.Equal([1, 2, 3, 255, 4, 5, 6, 255], rgb.Rgba);
        Assert.Equal([60, 70, 80, 255, 9, 8, 7, 255], palette.Rgba);
        Assert.Equal([50, 50, 50, 128, 51, 51, 51, 0], greyAlpha.Rgba);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], rgba.Rgba);
        Assert.All(new[] { grey, rgb, palette, greyAlpha, rgba }, picture => Assert.Equal((2, 1), (picture.Width, picture.Height)));
    }

    // Each filter of the PNG standard, on each row of a picture of 5 by 4 RGB pixels with bytes
    // that are not regular, gives back the same pixels; row 0 is the top row.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Each_Filter_Gives_Back_The_Pixels(byte filter)
    {
        var random = new Random(7 + filter);
        var rows = Enumerable.Range(0, 4).Select(_ => Enumerable.Range(0, 15).Select(_ => (byte)random.Next(256)).ToArray()).ToArray();

        var picture = PngReader.Read(Encode(5, 4, 2, rows, filter: filter));

        for (var row = 0; row < 4; row++)
            for (var x = 0; x < 5; x++)
                Assert.Equal([rows[row][x * 3], rows[row][x * 3 + 1], rows[row][x * 3 + 2], 255], picture.Rgba.AsSpan((row * 5 + x) * 4, 4).ToArray());
    }

    // The Paeth predictor of the PNG standard takes up before the corner on a tie. At the lower-right
    // pixel the left byte is 4, the byte above is 1 and the corner is 3: the distances to up and to
    // the corner are both 1, and random rows seldom give such a tie.
    [Fact]
    public void A_Tie_Of_The_Paeth_Predictor_Takes_Up_Before_The_Corner()
    {
        var picture = PngReader.Read(Encode(2, 2, 0, [[3, 1], [4, 200]], filter: 4));

        Assert.Equal([3, 1, 4, 200], Enumerable.Range(0, 4).Select(pixel => picture.Rgba[pixel * 4]));
    }

    public static TheoryData<string, byte[]> WrongFiles()
    {
        var good = Encode(2, 1, 2, [[1, 2, 3, 4, 5, 6]]);
        var badCrc = (byte[])good.Clone();
        badCrc[29] ^= 0xFF;
        var sixteenBits = Encode(1, 1, 2, [[0, 1, 0, 2, 0, 3]], depth: 16);
        return new TheoryData<string, byte[]>
        {
            { "signature", Encoding.ASCII.GetBytes("GIF89a and more bytes") },
            { "CRC", badCrc },
            { "8 only", sixteenBits },
            { "interlace", Encode(1, 1, 2, [[1, 2, 3]], interlace: 1) },
            { "from 1 to 4096", Encode(4097, 1, 0, [new byte[4097]]) },
            { "from 1 to 4096", Encode(0, 1, 0, []) },
            { "colour type 5", Encode(1, 1, 5, [[1]]) },
            { "no PLTE", Encode(1, 1, 3, [[0]]) },
            { "palette index 2", Encode(1, 1, 3, [[2]], palette: [1, 2, 3, 4, 5, 6]) },
            { "filter 5", Encode(1, 1, 2, [[1, 2, 3]], filter: 5) },
            { "shorter than its rows", Encode(2, 2, 2, [[1, 2, 3, 4, 5, 6]]) },
            { "ends inside a chunk", good[..^12] },
            { "longer than the rest of the file", good[..^14] },
        };
    }

    [Theory]
    [MemberData(nameof(WrongFiles))]
    public void A_File_Of_Another_Form_Is_Refused_With_Its_Reason(string reason, byte[] file)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => PngReader.Read(file));

        Assert.Contains(reason, refusal.Message, StringComparison.Ordinal);
    }

    // A PNG file with the given rows, each with one filter. Width and height go in IHDR as given, so
    // a test can make a header that the reader refuses.
    private static byte[] Encode(int width, int height, byte colourType, byte[][] rows, byte filter = 0, byte[]? palette = null, byte depth = 8, byte interlace = 0)
    {
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        (header[8], header[9], header[12]) = (depth, colourType, interlace);

        var channels = colourType switch { 2 => 3, 4 => 2, 6 => 4, _ => 1 } * (depth / 8);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            byte[]? above = null;
            foreach (var row in rows)
            {
                zlib.WriteByte(filter);
                zlib.Write(Filter(row, above, channels, filter));
                above = row;
            }
        }

        using var file = new MemoryStream();
        file.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
        Chunk(file, "IHDR", header);
        if (palette is not null)
            Chunk(file, "PLTE", palette);
        Chunk(file, "IDAT", compressed.ToArray());
        Chunk(file, "IEND", []);
        return file.ToArray();
    }

    private static byte[] Filter(byte[] row, byte[]? above, int channels, byte filter)
    {
        var result = new byte[row.Length];
        for (var i = 0; i < row.Length; i++)
        {
            int left = i >= channels ? row[i - channels] : 0;
            int up = above is null ? 0 : above[i];
            int corner = i >= channels && above is not null ? above[i - channels] : 0;
            var estimate = left + up - corner;
            var paeth = Math.Abs(estimate - left) <= Math.Abs(estimate - up) && Math.Abs(estimate - left) <= Math.Abs(estimate - corner)
                ? left : Math.Abs(estimate - up) <= Math.Abs(estimate - corner) ? up : corner;
            result[i] = filter switch
            {
                1 => (byte)(row[i] - left),
                2 => (byte)(row[i] - up),
                3 => (byte)(row[i] - (left + up) / 2),
                4 => (byte)(row[i] - paeth),
                _ => row[i],
            };
        }

        return result;
    }

    private static void Chunk(Stream stream, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        stream.Write(number);
        var typed = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        stream.Write(typed);
        var crc = 0xFFFFFFFFu;
        foreach (var value in typed)
        {
            crc ^= value;
            for (var k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }

        BinaryPrimitives.WriteUInt32BigEndian(number, crc ^ 0xFFFFFFFFu);
        stream.Write(number);
    }
}
