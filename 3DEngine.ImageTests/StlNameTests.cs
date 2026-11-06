using Engine.Api.Http;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0048 item 6, with no GPU: the name of the field of the export gives one file with the extension .stl
// in the document folder. Finding V72 of the codebase review of 2026-11-01 (TASK-0162): Resolve adds .3de,
// so until TASK-0162 the name "part.stl" gave the file "part.stl.stl".
public sealed class StlNameTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-stl-name-" + Guid.NewGuid().ToString("N"));

    public StlNameTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Theory]
    [InlineData("part")]
    [InlineData("part.3de")]
    [InlineData("part.stl")]
    [InlineData("part.STL")]
    public void Each_Name_Of_A_Part_Gives_One_Extension(string name)
    {
        var path = NativeThreeDEngine.StlPath(new DesktopDocument(_folder), name, out var refusal);

        Assert.Equal(Path.Combine(Path.GetFullPath(_folder), "part.stl"), path);
        Assert.Null(refusal);
    }

    // A name that is only the extension has no name before it, and the rules of a document file refuse it.
    [Fact]
    public void The_Extension_Alone_Is_Not_A_Name()
    {
        Assert.Null(NativeThreeDEngine.StlPath(new DesktopDocument(_folder), ".stl", out var refusal));
        Assert.Equal("A name is required.", refusal);
    }
}
