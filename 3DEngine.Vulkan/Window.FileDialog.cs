using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;
using static SDL.SDL3;

namespace ThreeDEngine.Vulkan;

// ADR-0083 D1 and D6 (phases P9c and P9d, TASK-0259): the file dialog of the system, through SDL. On Windows it is the file
// dialog of Windows, and on Android it is the document picker of Android, which needs no permission of storage. SDL calls
// the callback on a thread of its own, so the result waits in a field until the loop takes it. One dialog is open at a time.
public sealed unsafe partial class Window
{
    private static readonly object DialogLock = new();
    private static bool _dialogOpen;
    private static (bool Done, string? Path, string? Failure) _dialogResult;
    private static nint _filterName, _filterPattern, _filter;

    // Opens the dialog for one file with the filter, for example "STL file" and "stl". False when a dialog is open already.
    public bool OpenFile(string filterName, string pattern)
    {
        lock (DialogLock)
        {
            if (_dialogOpen)
                return false;
            _dialogOpen = true;
            _dialogResult = default;
            _filterName = Marshal.StringToCoTaskMemUTF8(filterName);
            _filterPattern = Marshal.StringToCoTaskMemUTF8(pattern);
            _filter = Marshal.AllocCoTaskMem(sizeof(SDL_DialogFileFilter));
            *(SDL_DialogFileFilter*)_filter = new SDL_DialogFileFilter { name = (byte*)_filterName, pattern = (byte*)_filterPattern };
        }

        SDL_ShowOpenFileDialog(&Chosen, nint.Zero, _window, (SDL_DialogFileFilter*)_filter, 1, (byte*)null, false);
        return true;
    }

    // The result of the dialog: Done is true one time after the dialog closes, with the path, or null when the person
    // closed it with no file or the dialog failed. Finding V131 of the codebase review of 2026-11-07 (b) (TASK-0271): Failure
    // gives the reason of SDL when the dialog failed, and null when the person closed it.
    public static (bool Done, string? Path, string? Failure) TakeOpenedFile()
    {
        lock (DialogLock)
        {
            var result = _dialogResult;
            if (result.Done)
                _dialogResult = default;
            return result;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Chosen(nint userdata, byte** files, int filter)
    {
        var path = files is not null && files[0] is not null ? Marshal.PtrToStringUTF8((nint)files[0]) : null;
        var failure = files is null ? Error() : null;
        lock (DialogLock)
        {
            _dialogResult = (true, path, failure);
            _dialogOpen = false;
            Marshal.FreeCoTaskMem(_filterName);
            Marshal.FreeCoTaskMem(_filterPattern);
            Marshal.FreeCoTaskMem(_filter);
            (_filterName, _filterPattern, _filter) = (0, 0, 0);
        }
    }

    // The bytes of the file at the path, through SDL, so a content URI of the document picker of Android reads too; null
    // with the text of SDL when the read fails.
    public static (byte[]? Content, string? Error) LoadFile(string path)
    {
        nuint size;
        var data = SDL_LoadFile(path, &size);
        if (data == nint.Zero)
            return (null, Error());
        try
        {
            if (size > int.MaxValue)
                return (null, $"The file has {size} bytes, more than one read holds.");
            var content = new byte[(int)size];
            Marshal.Copy(data, content, 0, (int)size);
            return (content, null);
        }
        finally
        {
            SDL_free(data);
        }
    }
}
