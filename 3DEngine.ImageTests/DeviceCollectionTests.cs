using System.Text.RegularExpressions;

namespace ThreeDEngine.ImageTests;

// R-0062 (TASK-0382): each file of the image tests that creates a Vulkan device puts its class in the collection of Dear
// ImGui, so no two devices are created at the same time. The test reads the source files of the project.
public sealed class DeviceCollectionTests
{
    // The text that creates a device, in two parts, so that this file does not hold it.
    private const string Creation = "new GraphicsDevice" + "(";

    [Fact]
    public void Each_Class_That_Creates_A_Device_Runs_In_The_Collection_Of_The_Devices()
    {
        var folder = Folder();
        var files = Directory.GetFiles(folder, "*.cs").Where(file => File.ReadAllText(file).Contains(Creation, StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(files);
        Assert.Empty(files.Where(file => !Regex.IsMatch(File.ReadAllText(file), @"\[Collection\(DearImGui\.Collection\)\]")).Select(Path.GetFileName));
    }

    // The folder of the source files of this project, found up from the folder of the test assembly.
    private static string Folder()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var candidate = Path.Combine(folder.FullName, "3DEngine.ImageTests");
            if (File.Exists(Path.Combine(candidate, "3DEngine.ImageTests.csproj")))
                return candidate;
        }

        throw new InvalidOperationException("The folder of 3DEngine.ImageTests is not found.");
    }
}
