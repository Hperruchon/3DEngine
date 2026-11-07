using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;
using static SDL.SDL3;

namespace ThreeDEngine.Vulkan;

// ADR-0083 D1 and D6 (phases P9c and P9d, TASK-0259): the file dialog of the system, through SDL. On Windows it is the file
// dialog of Windows, and on Android it is the document picker of Android, which needs no permission of storage. SDL calls
// the callback on a thread of its own, so the result waits in a field until the loop takes it. One dialog is open at a time.
// Finding V139 of the codebase review of 2026-11-07 (c) (TASK-0282): the state of the dialog, apart from SDL so that a test
// reaches it. Each dialog has its own number, which SDL gives back to the callback, and its own filter, which stays until
// that callback, because SDL may read it until then. A late callback of an earlier dialog frees its own filter and gives no
// result. Until TASK-0282 a late callback after ResetDialog freed the filter of the next dialog and cleared its flag, and
// the next OpenFile wrote over the filter of the earlier dialog with no free.
public static class FileDialogState
{
    private static readonly object Lock = new();
    private static readonly Dictionary<int, (nint Name, nint Pattern, nint Filter)> Filters = [];
    private static int _open;
    private static int _last;
    private static (bool Done, string? Path, string? Failure) _result;

    // The number and the filter of a new dialog, or null when a dialog is open already.
    public static unsafe (int Number, nint Filter)? Begin(string filterName, string pattern)
    {
        lock (Lock)
        {
            if (_open != 0)
                return null;
            _open = ++_last;
            _result = default;
            var name = Marshal.StringToCoTaskMemUTF8(filterName);
            var patternText = Marshal.StringToCoTaskMemUTF8(pattern);
            var filter = Marshal.AllocCoTaskMem(sizeof(SDL_DialogFileFilter));
            *(SDL_DialogFileFilter*)filter = new SDL_DialogFileFilter { name = (byte*)name, pattern = (byte*)patternText };
            Filters[_open] = (name, patternText, filter);
            return (_open, filter);
        }
    }

    // The callback of the dialog with this number: its filter is freed, and only the open dialog gives a result.
    public static void Finish(int number, string? path, string? failure)
    {
        lock (Lock)
        {
            if (Filters.Remove(number, out var memory))
            {
                Marshal.FreeCoTaskMem(memory.Name);
                Marshal.FreeCoTaskMem(memory.Pattern);
                Marshal.FreeCoTaskMem(memory.Filter);
            }

            if (number == _open)
            {
                _result = (true, path, failure);
                _open = 0;
            }
        }
    }

    // E179 (TASK-0272): a new window starts with no open dialog; the filter of an earlier dialog stays until its callback.
    public static void Reset()
    {
        lock (Lock)
        {
            _open = 0;
            _result = default;
        }
    }

    // Done is true one time after the dialog closes.
    public static (bool Done, string? Path, string? Failure) Take()
    {
        lock (Lock)
        {
            var result = _result;
            if (result.Done)
                _result = default;
            return result;
        }
    }

    // The filters that wait for their callback.
    public static int Waiting
    {
        get
        {
            lock (Lock)
                return Filters.Count;
        }
    }
}

public sealed unsafe partial class Window
{
    // Opens the dialog for one file with the filter, for example "STL file" and "stl". False when a dialog is open already.
    public bool OpenFile(string filterName, string pattern)
    {
        if (FileDialogState.Begin(filterName, pattern) is not { } dialog)
            return false;
        SDL_ShowOpenFileDialog(&Chosen, dialog.Number, _window, (SDL_DialogFileFilter*)dialog.Filter, 1, (byte*)null, false);
        return true;
    }

    // The result of the dialog: Done is true one time after the dialog closes, with the path, or null when the person
    // closed it with no file or the dialog failed. Finding V131 of the codebase review of 2026-11-07 (b) (TASK-0271): Failure
    // gives the reason of SDL when the dialog failed, and null when the person closed it.
    public static (bool Done, string? Path, string? Failure) TakeOpenedFile() => FileDialogState.Take();

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Chosen(nint userdata, byte** files, int filter)
    {
        var path = files is not null && files[0] is not null ? Marshal.PtrToStringUTF8((nint)files[0]) : null;
        var failure = files is null ? Error() : null;
        FileDialogState.Finish((int)userdata, path, failure);
    }

    // Finding E179 of the codebase review of 2026-11-07 (b) (TASK-0272): the state of the dialog belongs to the process, and
    // only the callback cleared it, so a callback that never came blocked "Open file" for the life of the process. Each new
    // window starts with no dialog.
    internal static void ResetDialog() => FileDialogState.Reset();

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
