using System.Runtime.InteropServices;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.ImageTests;

// Finding V139 of the codebase review of 2026-11-07 (c) (TASK-0282): a late callback of an earlier dialog frees its own filter
// and gives no result; the next dialog keeps its filter and its result. Until TASK-0282 the late callback freed the filter of
// the next dialog and cleared its flag, and the next dialog wrote over the earlier filter with no free.
[Collection(Collection)]
public sealed class FileDialogStateTests
{
    // TASK-0292: the collection of the classes that change the state of the file dialog of the process.
    public const string Collection = "The state of the file dialog";

    [Fact]
    public void A_Late_Callback_Of_An_Earlier_Dialog_Touches_Only_Its_Own_Filter()
    {
        FileDialogState.Reset();
        var waiting = FileDialogState.Waiting;
        var first = FileDialogState.Begin("STL file", "stl") ?? throw new InvalidOperationException("No first dialog.");
        Assert.Null(FileDialogState.Begin("STL file", "stl"));

        // A new window starts with no open dialog, while the callback of the first one has not come.
        FileDialogState.Reset();
        var second = FileDialogState.Begin("STL file", "stl") ?? throw new InvalidOperationException("No second dialog.");
        Assert.NotEqual(first.Number, second.Number);
        Assert.NotEqual(first.Filter, second.Filter);
        Assert.Equal(waiting + 2, FileDialogState.Waiting);

        FileDialogState.Finish(first.Number, "old.stl", null);
        Assert.False(FileDialogState.Take().Done);
        Assert.Equal(waiting + 1, FileDialogState.Waiting);
        Assert.Null(FileDialogState.Begin("STL file", "stl"));

        FileDialogState.Finish(second.Number, "new.stl", null);
        Assert.Equal((true, "new.stl", (string?)null, 0), FileDialogState.Take());
        Assert.Equal(waiting, FileDialogState.Waiting);
    }

    // ADR-0102 item 2 (phase P9g, TASK-0316): the result gives back the purpose of its dialog, and a late callback of an
    // earlier dialog with another purpose gives no result; the default location of a save dialog is freed with its filter.
    [Fact]
    public void The_Result_Gives_The_Purpose_Of_Its_Own_Dialog()
    {
        FileDialogState.Reset();
        var waiting = FileDialogState.Waiting;
        var import = FileDialogState.Begin("STL file", "stl", purpose: 0) ?? throw new InvalidOperationException("No import dialog.");
        FileDialogState.Reset();
        var save = FileDialogState.Begin("3DEngine project", "3de", purpose: 2, location: "C:/projects/shelf.3de") ?? throw new InvalidOperationException("No save dialog.");
        Assert.NotEqual(0, save.Location);
        Assert.Equal(0, import.Location);

        FileDialogState.Finish(import.Number, "late.stl", null);
        Assert.False(FileDialogState.Take().Done);
        FileDialogState.Finish(save.Number, "C:/projects/shelf.3de", null);

        Assert.Equal((true, "C:/projects/shelf.3de", (string?)null, 2), FileDialogState.Take());
        Assert.Equal(waiting, FileDialogState.Waiting);
    }

    // ADR-0106 (phase P0.137, TASK-0328): a dialog with three filters gives SDL an array of three, in their order, and the
    // callback frees each name and each pattern with the array.
    [Fact]
    public unsafe void A_Dialog_Gives_Its_Filters_In_Their_Order()
    {
        FileDialogState.Reset();
        var waiting = FileDialogState.Waiting;
        var open = FileDialogState.Begin([("Project or STL", "3de;stl"), ("3DEngine project", "3de"), ("STL file", "stl")], purpose: 1)
            ?? throw new InvalidOperationException("No open dialog.");

        var filters = (SDL.SDL_DialogFileFilter*)open.Filters;
        var read = Enumerable.Range(0, open.Count)
            .Select(index => (Marshal.PtrToStringUTF8((nint)filters[index].name), Marshal.PtrToStringUTF8((nint)filters[index].pattern)))
            .ToList();
        Assert.Equal([("Project or STL", "3de;stl"), ("3DEngine project", "3de"), ("STL file", "stl")], read);

        FileDialogState.Finish(open.Number, "C:/Downloads/bracket.stl", null);
        Assert.Equal((true, "C:/Downloads/bracket.stl", (string?)null, 1), FileDialogState.Take());
        Assert.Equal(waiting, FileDialogState.Waiting);
    }
}
