using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop.Ui;

// The draw data of Dear ImGui in the neutral form of the renderer (ADR-0027 item 3,
// TASK-0084). The lists of the library become one array of vertices and one array of
// indices; each command keeps its clip rectangle and its texture, and names its range in
// the arrays of the frame. The vertex of the library and UiVertex have one layout, so the
// bytes are copied.
internal static unsafe class UiDrawDataConverter
{
    // True when the vertex of the library has the layout of UiVertex: 20 bytes, with the
    // colour at offset 16. The converter refuses to copy when it is false.
    public static bool VertexLayoutAgrees
        => Unsafe.SizeOf<ImDrawVert>() == (int)UiVertex.Stride
            && Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.uv)) == 8
            && Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.col)) == 16
            && sizeof(ushort) == Unsafe.SizeOf<ushort>();

    public static UiDrawData Convert(ImDrawDataPtr data)
    {
        if (!VertexLayoutAgrees)
            throw new InvalidOperationException("The vertex of Dear ImGui does not have the layout of UiVertex.");
        if (data.NativePtr == null || data.CmdListsCount == 0 || data.TotalVtxCount == 0)
            return UiDrawData.Empty;

        var vertices = new UiVertex[data.TotalVtxCount];
        var indices = new ushort[data.TotalIdxCount];
        var commands = new List<UiDrawCommand>();
        var vertexStart = 0;
        var indexStart = 0;

        for (var listIndex = 0; listIndex < data.CmdListsCount; listIndex++)
        {
            var list = data.CmdLists[listIndex];
            new ReadOnlySpan<UiVertex>((void*)list.VtxBuffer.Data, list.VtxBuffer.Size).CopyTo(vertices.AsSpan(vertexStart));
            new ReadOnlySpan<ushort>((void*)list.IdxBuffer.Data, list.IdxBuffer.Size).CopyTo(indices.AsSpan(indexStart));

            for (var commandIndex = 0; commandIndex < list.CmdBuffer.Size; commandIndex++)
            {
                var command = list.CmdBuffer[commandIndex];

                // This renderer sets no callback, so a command with one is a defect.
                if (command.UserCallback != IntPtr.Zero)
                    throw new InvalidOperationException("A command of Dear ImGui has a callback, which the renderer does not run.");

                commands.Add(new UiDrawCommand(
                    command.ClipRect,
                    (ulong)command.TextureId,
                    (uint)indexStart + command.IdxOffset,
                    command.ElemCount,
                    vertexStart + (int)command.VtxOffset));
            }

            vertexStart += list.VtxBuffer.Size;
            indexStart += list.IdxBuffer.Size;
        }

        return new UiDrawData(data.DisplayPos, data.DisplaySize, data.FramebufferScale, vertices, indices, [.. commands]);
    }
}
