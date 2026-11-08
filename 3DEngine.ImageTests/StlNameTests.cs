using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0048 item 6, with no GPU: an export gives one file with the extension .stl. Finding V72 of the codebase review of
// 2026-11-01 (TASK-0162): until TASK-0162 the name "part.stl" gave the file "part.stl.stl". ADR-0100 (TASK-0314): the path
// comes from the save dialog of the system, and a save of a project gets .3de the same way.
public sealed class StlNameTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-stl-name-" + Guid.NewGuid().ToString("N"));

    public StlNameTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // ADR-0100 items 3 and 4 (phase P9g, TASK-0314): a path of a dialog of the system gets its extension once. Until TASK-0314
    // the test held the name of the field in the document folder; finding V72 (TASK-0162) was "part.stl.stl".
    [Theory]
    [InlineData("part", ".stl", "part.stl")]
    [InlineData("part.stl", ".stl", "part.stl")]
    [InlineData("part.STL", ".stl", "part.STL")]
    [InlineData("shelf", ".3de", "shelf.3de")]
    [InlineData("shelf.3DE", ".3de", "shelf.3DE")]
    public void Each_Path_Of_A_Dialog_Gets_Its_Extension_Once(string name, string extension, string expected)
    {
        Assert.Equal(Path.Combine(_folder, expected), ProjectFiles.WithExtension(Path.Combine(_folder, name), extension));
    }
}
