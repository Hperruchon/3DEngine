namespace ThreeDEngine.Desktop;

// ADR-0100 item 6 (Q2 A, phase P9g, TASK-0314): the last eight projects that this desktop opened or saved, in each folder,
// newest first, one full path on each line of a text file of the local application data. The list is a setting of the
// desktop and not a part of a Document. A path that no longer exists leaves the list when the list is read. A read or a
// write that fails gives an empty list or keeps the old list: the recent rows are a help, so their failure stops nothing.
internal sealed class RecentProjects(string file)
{
    // The count of the paths that the list keeps.
    public const int Capacity = 8;

    // The name of the file of the list.
    public const string FileName = "recent-projects.txt";

    // The file of the list.
    public string File { get; } = file;

    // The file of the list beside the folder of unsaved documents: by default both are in "<local application data>/3DEngine",
    // and a test that gives a scratch folder of unsaved documents gets a scratch list too.
    public static string Beside(string unsavedFolder)
    {
        var folder = Path.GetFullPath(unsavedFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.Combine(Path.GetDirectoryName(folder) ?? folder, FileName);
    }

    // The paths that still exist, newest first, at most count.
    public IReadOnlyList<string> Paths(int count)
    {
        var all = Read();
        var live = all.Where(System.IO.File.Exists).ToList();
        if (live.Count != all.Count)
            Write(live);
        return [.. live.Take(count)];
    }

    // The path goes to the top of the list once, and the list keeps eight paths.
    public void Remember(string path)
    {
        var full = Path.GetFullPath(path);
        Write([.. Read().Where(known => !DesktopDocument.SameFile(known, full)).Prepend(full).Take(Capacity)]);
    }

    private List<string> Read()
    {
        try
        {
            return System.IO.File.Exists(File) ? [.. System.IO.File.ReadAllLines(File).Where(line => line.Length > 0)] : [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private void Write(IReadOnlyList<string> paths)
    {
        try
        {
            if (Path.GetDirectoryName(File) is { Length: > 0 } folder)
                Directory.CreateDirectory(folder);
            System.IO.File.WriteAllLines(File, paths);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The list keeps its old paths.
        }
    }
}
