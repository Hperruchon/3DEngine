using System.Runtime.InteropServices;
using System.Text;

namespace ThreeDEngine.Vulkan;

// Names for a call of Vulkan: each one a UTF-8 string with a zero at its end, in
// native memory, and an array of their pointers. The garbage collector does not move
// native memory, so the pointers stay valid until Dispose. Before TASK-0076 each name
// was a managed byte array or a pointer from a fixed block that ended before the call,
// and the collector could move or free it before Vulkan read it (R-0030, finding V6).
internal sealed unsafe class NativeStrings : IDisposable
{
    private readonly byte** _pointers;

    public NativeStrings(IReadOnlyList<string> values)
    {
        Count = (uint)values.Count;
        _pointers = (byte**)NativeMemory.AllocZeroed((nuint)Math.Max(1, values.Count), (nuint)sizeof(byte*));
        for (var index = 0; index < values.Count; index++)
        {
            var bytes = Encoding.UTF8.GetBytes(values[index]);
            var text = (byte*)NativeMemory.Alloc((nuint)bytes.Length + 1);
            bytes.CopyTo(new Span<byte>(text, bytes.Length));
            text[bytes.Length] = 0;
            _pointers[index] = text;
        }
    }

    public uint Count { get; }

    // The array of the pointers, for a field such as ppEnabledExtensionNames.
    public byte** Pointers => _pointers;

    // The first string, for a field such as pApplicationName.
    public byte* First => _pointers[0];

    // A copy of a name that Vulkan wrote into a fixed buffer of a structure. The copy
    // is managed, so it does not keep a pointer into that buffer.
    public static string Read(byte* name) => Marshal.PtrToStringUTF8((nint)name) ?? string.Empty;

    public void Dispose()
    {
        for (var index = 0; index < Count; index++)
            NativeMemory.Free(_pointers[index]);
        NativeMemory.Free(_pointers);
    }
}
