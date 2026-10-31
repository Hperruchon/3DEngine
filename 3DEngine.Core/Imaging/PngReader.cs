using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace ThreeDEngine.Core.Imaging;

// The pixels of a picture: 8-bit RGBA, the top row first, as ImageRenderer takes them.
public sealed record PngPicture(int Width, int Height, byte[] Rgba);

// ADR-0046 item 1: reads a PNG file of 8 bits for each channel with no interlace, in each of the
// five colour types: grey, RGB, palette, grey with alpha and RGBA. Each chunk has its CRC, and the
// rows have the five filters of the PNG standard. The alpha of a pixel is kept, but the image of
// ADR-0043 is opaque. A file of another form throws InvalidDataException with the reason, so the
// desktop can show it.
public static class PngReader
{
    // Vulkan gives each device at least 4096 for maxImageDimension2D, so a side up to it draws on
    // each device.
    public const int MaximumSide = 4096;

    private static readonly byte[] Signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static PngPicture Read(ReadOnlySpan<byte> file)
    {
        if (file.Length < Signature.Length || !file[..Signature.Length].SequenceEqual(Signature))
            throw new InvalidDataException("The file is not a PNG file: its first 8 bytes are not the signature of PNG.");

        var position = Signature.Length;
        (int Width, int Height, int Channels, byte ColourType)? header = null;
        byte[]? palette = null;
        using var compressed = new MemoryStream();
        var ended = false;
        while (!ended)
        {
            if (file.Length - position < 12)
                throw new InvalidDataException("The PNG file ends inside a chunk, before its IEND chunk.");
            var length = BinaryPrimitives.ReadUInt32BigEndian(file[position..]);
            if (length > (uint)(file.Length - position - 12))
                throw new InvalidDataException("A chunk of the PNG file is longer than the rest of the file.");
            var type = Encoding.ASCII.GetString(file.Slice(position + 4, 4));
            var data = file.Slice(position + 8, (int)length);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(file[(position + 8 + (int)length)..]);
            if (Crc(file.Slice(position + 4, 4 + (int)length)) != crc)
                throw new InvalidDataException($"The CRC of the chunk {type} of the PNG file is wrong.");
            position += 12 + (int)length;

            if (header is null && type != "IHDR")
                throw new InvalidDataException("The first chunk of the PNG file is not IHDR.");
            switch (type)
            {
                case "IHDR":
                    if (header is not null)
                        throw new InvalidDataException("The PNG file has two IHDR chunks.");
                    header = Header(data);
                    break;
                case "PLTE":
                    if (data.Length == 0 || data.Length % 3 != 0 || data.Length > 768)
                        throw new InvalidDataException("The palette of the PNG file is not 1 to 256 colours of 3 bytes.");
                    palette = data.ToArray();
                    break;
                case "IDAT":
                    compressed.Write(data);
                    break;
                case "IEND":
                    ended = true;
                    break;
            }
        }

        var (width, height, channels, colourType) = header!.Value;
        if (colourType == 3 && palette is null)
            throw new InvalidDataException("The PNG file has the colour type palette and no PLTE chunk.");

        var stride = width * channels;
        var rows = Inflate(compressed, height * (stride + 1));
        Unfilter(rows, height, stride, channels);
        return new PngPicture(width, height, Rgba(rows, width, height, stride, colourType, palette));
    }

    private static (int Width, int Height, int Channels, byte ColourType) Header(ReadOnlySpan<byte> data)
    {
        if (data.Length != 13)
            throw new InvalidDataException("The IHDR chunk of the PNG file does not have 13 bytes.");
        var width = BinaryPrimitives.ReadInt32BigEndian(data);
        var height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
        if (width is < 1 or > MaximumSide || height is < 1 or > MaximumSide)
            throw new InvalidDataException($"The picture is {width} by {height} pixels; each side must be from 1 to {MaximumSide} pixels.");
        if (data[8] != 8)
            throw new InvalidDataException($"The PNG file has {data[8]} bits for each channel; the desktop reads 8 only.");
        var channels = data[9] switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new InvalidDataException($"The colour type {data[9]} is not a colour type of PNG."),
        };
        if (data[10] != 0 || data[11] != 0)
            throw new InvalidDataException("The PNG file has a method of compression or of filter that PNG does not define.");
        if (data[12] != 0)
            throw new InvalidDataException("The PNG file is interlaced; the desktop reads a file with no interlace only.");
        return (width, height, channels, data[9]);
    }

    private static byte[] Inflate(MemoryStream compressed, int size)
    {
        compressed.Position = 0;
        var rows = new byte[size];
        try
        {
            using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
            zlib.ReadExactly(rows);
            if (zlib.ReadByte() != -1)
                throw new InvalidDataException("The image data of the PNG file is longer than its rows.");
        }
        catch (EndOfStreamException)
        {
            throw new InvalidDataException("The image data of the PNG file is shorter than its rows.");
        }

        return rows;
    }

    // The five filters of the PNG standard, in place. Each row starts with the byte of its filter.
    private static void Unfilter(byte[] rows, int height, int stride, int channels)
    {
        for (var row = 0; row < height; row++)
        {
            var start = row * (stride + 1);
            var filter = rows[start];
            var line = rows.AsSpan(start + 1, stride);
            var above = row == 0 ? Span<byte>.Empty : rows.AsSpan(start - stride, stride);
            for (var i = 0; i < stride; i++)
            {
                int left = i >= channels ? line[i - channels] : 0;
                int up = above.IsEmpty ? 0 : above[i];
                int corner = i >= channels && !above.IsEmpty ? above[i - channels] : 0;
                line[i] = filter switch
                {
                    0 => line[i],
                    1 => (byte)(line[i] + left),
                    2 => (byte)(line[i] + up),
                    3 => (byte)(line[i] + (left + up) / 2),
                    4 => (byte)(line[i] + Paeth(left, up, corner)),
                    _ => throw new InvalidDataException($"Row {row} of the PNG file has the filter {filter}, which PNG does not define."),
                };
            }
        }
    }

    private static int Paeth(int left, int up, int corner)
    {
        var estimate = left + up - corner;
        var (toLeft, toUp, toCorner) = (Math.Abs(estimate - left), Math.Abs(estimate - up), Math.Abs(estimate - corner));
        return toLeft <= toUp && toLeft <= toCorner ? left : toUp <= toCorner ? up : corner;
    }

    private static byte[] Rgba(byte[] rows, int width, int height, int stride, byte colourType, byte[]? palette)
    {
        var rgba = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
        {
            var line = rows.AsSpan(row * (stride + 1) + 1, stride);
            for (var x = 0; x < width; x++)
            {
                var pixel = rgba.AsSpan((row * width + x) * 4, 4);
                switch (colourType)
                {
                    case 0:
                        pixel[0] = pixel[1] = pixel[2] = line[x];
                        pixel[3] = 255;
                        break;
                    case 2:
                        line.Slice(x * 3, 3).CopyTo(pixel);
                        pixel[3] = 255;
                        break;
                    case 3:
                        var index = line[x] * 3;
                        if (index + 3 > palette!.Length)
                            throw new InvalidDataException($"A pixel of the PNG file has the palette index {line[x]}, and the palette has {palette.Length / 3} colours.");
                        palette.AsSpan(index, 3).CopyTo(pixel);
                        pixel[3] = 255;
                        break;
                    case 4:
                        pixel[0] = pixel[1] = pixel[2] = line[x * 2];
                        pixel[3] = line[x * 2 + 1];
                        break;
                    default:
                        line.Slice(x * 4, 4).CopyTo(pixel);
                        break;
                }
            }
        }

        return rgba;
    }

    private static uint Crc(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }

        return table;
    }
}
