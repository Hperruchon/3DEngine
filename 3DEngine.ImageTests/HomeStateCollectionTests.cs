using System.Text.RegularExpressions;

namespace ThreeDEngine.ImageTests;

// Phase P0.114 (TASK-0278): the home screen and the pending import of the desktop belong to the process (static fields of
// DesktopDocument), so two test classes that change them must not run at the same time. xUnit runs two collections in
// parallel, so each such class is in the collection of DearImGui. Until TASK-0278 DesktopSessionTests had its own
// collection, and on macOS its open ended the home screen while StlOpenTests waited for it (run 42371538462).
public sealed class HomeStateCollectionTests
{
    // A class that uses the desktop document of the process and changes or reads its state.
    private static readonly Regex Uses = new(@"DesktopDocument\b|RunSessions\(", RegexOptions.CultureInvariant);
    private static readonly Regex Changes = new(@"RunSessions\(|\.Home\b|RequestNew\(|TakeImport\(|DropImport\(|\.Open\(", RegexOptions.CultureInvariant);

    [Fact]
    public void Each_Class_That_Changes_The_Home_State_Is_In_One_Collection()
    {
        var files = Directory.GetFiles(Path.Combine(RepositoryRoot(), "3DEngine.ImageTests"), "*.cs")
            .Where(file => !file.EndsWith(nameof(HomeStateCollectionTests) + ".cs", StringComparison.Ordinal)).ToList();
        var offences = files
            .Where(file => File.ReadAllText(file) is var text && Uses.IsMatch(text) && Changes.IsMatch(text)
                && !text.Contains("[Collection(DearImGui.Collection)]", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        // The guard reads the classes that it must read: the two that the run of macOS put at the same time.
        Assert.Contains(files, file => file.EndsWith("StlOpenTests.cs", StringComparison.Ordinal));
        Assert.Contains(files, file => file.EndsWith("DesktopSessionTests.cs", StringComparison.Ordinal));
        Assert.True(offences.Count == 0, $"Each class that changes the home state of the process must be in the collection of DearImGui: {string.Join(", ", offences)}.");
    }

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
