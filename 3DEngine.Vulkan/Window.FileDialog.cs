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

    // Finding E179 of the codebase review of 2026-11-07 (b) (TASK-0272): the state of the dialog belongs to the process, and
    // only the callback cleared it, so a callback that never came blocked "Open file" for the life of the process. Each new
    // window starts with no dialog; a late callback of an earlier window gives its result to the next take.
    internal static void ResetDialog()
    {
        lock (DialogLock)
        {
            _dialogOpen = false;
            _dialogResult = default;
        }
    }

    // Finding N2 of the review of Codex after v0.243 (TASK-0272): the size of the file at the path, through SDL, or -1
    // when SDL cannot open it, so that a file that is too large is refused before SDL reads it into memory.
    public static long FileSize(string path)
    {
        var stream = SDL_IOFromFile(path, "rb");
        if (stream is null)
            return -1;
        try
        {
            return SDL_GetIOSize(stream);
        }
        finally
        {
            SDL_CloseIO(stream);
        }
    }

    // The bytes of the file at the path, through SDL, so a content URI of the document picker of Android reads too; null
    // with the text of SDL when the read fails. Finding E183 of the codebase review of 2026-11-07 (c) (TASK-0281): the read
    // stops after maximum bytes and gives null with a text, so a file whose size SDL does not know cannot fill the memory.
    public static (byte[]? Content, string? Error) LoadFile(string path, long maximum)
    {
        var stream = SDL_IOFromFile(path, "rb");
        if (stream is null)
            return (null, Error());
        try
        {
            using var content = new MemoryStream();
            var buffer = new byte[64 * 1024];
            while (true)
            {
                nuint read;
                fixed (byte* at = buffer)
                    read = SDL_ReadIO(stream, (nint)at, (nuint)buffer.Length);
                if (read == 0)
                    break;
                content.Write(buffer, 0, (int)read);
                if (content.Length > maximum)
                    return (null, $"The file has more than {maximum} bytes.");
            }

            return SDL_GetIOStatus(stream) == SDL_IOStatus.SDL_IO_STATUS_ERROR ? (null, Error()) : (content.ToArray(), null);
        }
        finally
        {
            SDL_CloseIO(stream);
        }
    }
}
