using System.Reflection;

namespace ThreeDEngine.Vulkan;

// The compiled shaders of this project (ADR-0022). Each SPIR-V file of Shaders/ is
// an embedded resource with the name Shaders/<shader>.spv, so the program needs no
// file beside it. Shaders/compile.js makes each SPIR-V file, and the test
// ShaderSourceTests of 3DEngine.Tests holds each one equal to its source.
public static class ShaderLibrary
{
    // The first word of each SPIR-V module, in the byte order of the file.
    public const uint SpirvMagic = 0x07230203;

    private const string Prefix = "Shaders/";

    // The name of each shader, for example "triangle.vert".
    public static IReadOnlyList<string> Names { get; } = typeof(ShaderLibrary).Assembly
        .GetManifestResourceNames()
        .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(".spv", StringComparison.Ordinal))
        .Select(name => name[Prefix.Length..^".spv".Length])
        .Order(StringComparer.Ordinal)
        .ToArray();

    // The SPIR-V of the named shader, for example "triangle.vert".
    public static byte[] Load(string shader)
    {
        using var stream = typeof(ShaderLibrary).Assembly.GetManifestResourceStream(Prefix + shader + ".spv")
            ?? throw new ArgumentException($"No compiled shader '{shader}' is in {typeof(ShaderLibrary).Assembly.GetName().Name}.", nameof(shader));
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    // A SPIR-V module is a sequence of 32-bit words, and its first word is the magic
    // number in the byte order of the module. Vulkan reads it as little-endian words.
    public static bool HasSpirvHeader(ReadOnlySpan<byte> spirv)
        => spirv.Length >= 20 && spirv.Length % 4 == 0 && BitConverter.ToUInt32(spirv[..4]) == SpirvMagic;
}
