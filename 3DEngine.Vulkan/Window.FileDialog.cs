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
// ADR-0102 item 2 (phase P9g, TASK-0316): each dialog keeps the purpose that the client gives it, and its result gives that
// purpose back, so a late result of one action never goes to another action. A save dialog keeps its default location
// until its callback too, because SDL may read it until then.
// ADR-0106 (phase P0.137, TASK-0328): a dialog has one or more filters, in their order, and each name and pattern stays until
// the callback, as the single filter did.
public static class FileDialogState
{
    private static readonly object Lock = new();
    private static readonly Dictionary<int, (nint[] Texts, nint Filter, nint Location, int Purpose)> Filters = [];
    private static int _open;
    private static int _last;
    private static (bool Done, string? Path, string? Failure, int Purpose) _result;

    // The number, the filter and the default location (or 0) of a new dialog, or null when a dialog is open already.
    public static (int Number, nint Filter, nint Location)? Begin(string filterName, string pattern, int purpose = 0, string? location = null)
        => Begin([(filterName, pattern)], purpose, location) is { } dialog ? (dialog.Number, dialog.Filters, dialog.Location) : null;

    // ADR-0106: the number, the array of the filters in their order, their count and the default location (or 0) of a new
    // dialog, or null when a dialog is open already.
    public static unsafe (int Number, nint Filters, int Count, nint Location)? Begin(IReadOnlyList<(string Name, string Pattern)> filters, int purpose = 0, string? location = null)
    {
        ArgumentNullException.ThrowIfNull(filters);
        if (filters.Count == 0)
            throw new ArgumentException("A dialog needs at least one filter.", nameof(filters));
        lock (Lock)
        {
            if (_open != 0)
                return null;
            _open = ++_last;
            _result = default;
            var texts = new nint[filters.Count * 2];
            var array = Marshal.AllocCoTaskMem(sizeof(SDL_DialogFileFilter) * filters.Count);
            new Span<byte>((void*)array, sizeof(SDL_DialogFileFilter) * filters.Count).Clear();
            for (var index = 0; index < filters.Count; index++)
            {
                texts[2 * index] = Marshal.StringToCoTaskMemUTF8(filters[index].Name);
                texts[2 * index + 1] = Marshal.StringToCoTaskMemUTF8(filters[index].Pattern);
                ((SDL_DialogFileFilter*)array)[index] = new SDL_DialogFileFilter { name = (byte*)texts[2 * index], pattern = (byte*)texts[2 * index + 1] };
            }

            var place = location is null ? 0 : Marshal.StringToCoTaskMemUTF8(location);
            Filters[_open] = (texts, array, place, purpose);
            return (_open, array, filters.Count, place);
        }
    }

    // The callback of the dialog with this number: its filter is freed, and only the open dialog gives a result.
    public static void Finish(int number, string? path, string? failure)
    {
        lock (Lock)
        {
            var purpose = 0;
            if (Filters.Remove(number, out var memory))
            {
                foreach (var text in memory.Texts)
                    Marshal.FreeCoTaskMem(text);
                Marshal.FreeCoTaskMem(memory.Filter);
                if (memory.Location != 0)
                    Marshal.FreeCoTaskMem(memory.Location);
                purpose = memory.Purpose;
            }

            if (number == _open)
            {
                _result = (true, path, failure, purpose);
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

    // Done is true one time after the dialog closes, with the purpose of the dialog.
    public static (bool Done, string? Path, string? Failure, int Purpose) Take()
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
    // ADR-0102 item 2 (TASK-0316): the purpose comes back with the result.
    public bool OpenFile(string filterName, string pattern, int purpose = 0) => OpenFile([(filterName, pattern)], purpose);

    // ADR-0106 (TASK-0328): the dialog for one file with the filters in their order; the system shows the first one first.
    public bool OpenFile(IReadOnlyList<(string Name, string Pattern)> filters, int purpose)
    {
        if (FileDialogState.Begin(filters, purpose) is not { } dialog)
            return false;
        SDL_ShowOpenFileDialog(&Chosen, dialog.Number, _window, (SDL_DialogFileFilter*)dialog.Filters, dialog.Count, (byte*)null, false);
        return true;
    }

    // ADR-0102 items 3 and 4 (TASK-0316): the save dialog of the system for one file with the filter, in each folder, at the
    // default location (a file or a folder), or where the system chooses with null. False when a dialog is open already.
    public bool SaveFile(string filterName, string pattern, int purpose, string? location)
    {
        if (FileDialogState.Begin(filterName, pattern, purpose, location) is not { } dialog)
            return false;
        SDL_ShowSaveFileDialog(&Chosen, dialog.Number, _window, (SDL_DialogFileFilter*)dialog.Filter, 1, (byte*)dialog.Location);
        return true;
    }

    // The result of the dialog: Done is true one time after the dialog closes, with the path, or null when the person
    // closed it with no file or the dialog failed. Finding V131 of the codebase review of 2026-11-07 (b) (TASK-0271): Failure
    // gives the reason of SDL when the dialog failed, and null when the person closed it. ADR-0102 item 2: Purpose is the
    // purpose of the dialog.
    public static (bool Done, string? Path, string? Failure, int Purpose) TakeOpenedFile() => FileDialogState.Take();

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
    // Finding E190 of the codebase review of 2026-11-08 (TASK-0295): OverLimit is true for that stop, so the client gives the
    // code of a file too large, and not a failed read with no code.
    public static (byte[]? Content, string? Error, bool OverLimit) LoadFile(string path, long maximum)
    {
        var stream = SDL_IOFromFile(path, "rb");
        if (stream is null)
            return (null, Error(), false);
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
                    return (null, $"The file has more than {maximum} bytes.", true);
            }

            return SDL_GetIOStatus(stream) == SDL_IOStatus.SDL_IO_STATUS_ERROR ? (null, Error(), false) : (content.ToArray(), null, false);
        }
        finally
        {
            SDL_CloseIO(stream);
        }
    }
}
