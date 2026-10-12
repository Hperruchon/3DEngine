using System.Security.Cryptography;
using System.Text;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Tests;

// ADR-0022 item 1: each shader has its GLSL source and its SPIR-V in
// 3DEngine.Vulkan/Shaders/, and shaders.lock gives the SHA-256 of each one. No
// runner has a shader compiler, so this test is the check that a source and its
// SPIR-V agree: a change of a source with no new compile changes the hash of the
// source and fails it. Shaders/compile.js compiles each shader and writes the lock.
public class ShaderSourceTests
{
    private static readonly string Folder = Path.Combine(RepositoryRoot(), "3DEngine.Vulkan", "Shaders");

    [Fact]
    public void Each_Source_And_Each_SpirV_File_Agree_With_The_Lock()
    {
        var problems = new List<string>();

        foreach (var (shader, sourceHash, spirvHash) in LockLines())
        {
            var source = File.ReadAllText(Path.Combine(Folder, shader)).Replace("\r\n", "\n", StringComparison.Ordinal);
            if (Sha256(Encoding.UTF8.GetBytes(source)) != sourceHash)
                problems.Add($"{shader}: the source differs from the lock; run node 3DEngine.Vulkan/Shaders/compile.js");

            var spirvPath = Path.Combine(Folder, shader + ".spv");
            if (!File.Exists(spirvPath) || Sha256(File.ReadAllBytes(spirvPath)) != spirvHash)
                problems.Add($"{shader}.spv: the SPIR-V is absent or differs from the lock");
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void Each_Shader_Of_The_Folder_Has_A_Line_And_Each_Embedded_Module_Is_The_File()
    {
        var locked = LockLines().Select(line => line.Shader).ToHashSet(StringComparer.Ordinal);
        var sources = Directory.EnumerateFiles(Folder)
            .Select(Path.GetFileName)
            .Where(name => name!.EndsWith(".vert", StringComparison.Ordinal) || name.EndsWith(".frag", StringComparison.Ordinal))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(sources.Order(StringComparer.Ordinal), locked.Order(StringComparer.Ordinal));
        Assert.Equal(sources.Order(StringComparer.Ordinal), ShaderLibrary.Names);

        foreach (var shader in ShaderLibrary.Names)
        {
            var embedded = ShaderLibrary.Load(shader);
            Assert.True(ShaderLibrary.HasSpirvHeader(embedded), $"{shader}: the embedded module has no SPIR-V header");
            Assert.Equal(File.ReadAllBytes(Path.Combine(Folder, shader + ".spv")), embedded);
        }
    }

    [Theory]
    [InlineData(new byte[] { 0x03, 0x02, 0x23, 0x07, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 }, true)]
    [InlineData(new byte[] { 0x07, 0x23, 0x02, 0x03, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 }, false)]
    [InlineData(new byte[] { 0x03, 0x02, 0x23, 0x07 }, false)]
    [InlineData(new byte[] { 0x03, 0x02, 0x23, 0x07, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 }, false)]
    public void The_Header_Check_Reads_The_Magic_Number_In_Little_Endian_Words(byte[] bytes, bool valid)
    {
        Assert.Equal(valid, ShaderLibrary.HasSpirvHeader(bytes));
    }

    private static IEnumerable<(string Shader, string SourceHash, string SpirvHash)> LockLines()
        => File.ReadAllLines(Path.Combine(Folder, "shaders.lock"))
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split(' '))
            .Select(parts => (parts[0], parts[1], parts[2]));

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "3DEngine.sln")))
                return directory.FullName;
        }

        throw new InvalidOperationException("No 3DEngine.sln above the folder of the tests.");
    }
}
