using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The neutral 2D draw data of the user interface (ADR-0027 item 3, TASK-0084). The host
// turns the draw data of its library into this form, so that this project names no
// library of a user interface (ADR-0017 item 2).

// One vertex: a position in pixels of the display with y down, a texture coordinate, and
// a colour of four bytes in the order red, green, blue, alpha. The layout is the layout of
// the vertex of Dear ImGui, so that the host can copy the bytes; the host checks the size.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct UiVertex(Vector2 Position, Vector2 TextureCoordinate, uint Colour)
{
    public const uint Stride = 20;

    public static VkVertexInputBindingDescription Binding => new()
    {
        binding = 0,
        stride = Stride,
        inputRate = VkVertexInputRate.Vertex,
    };

    public static VkVertexInputAttributeDescription[] Attributes =>
    [
        new() { location = 0, binding = 0, format = VkFormat.R32G32Sfloat, offset = 0 },
        new() { location = 1, binding = 0, format = VkFormat.R32G32Sfloat, offset = 8 },
        new() { location = 2, binding = 0, format = VkFormat.R8G8B8A8Unorm, offset = 16 },
    ];
}

// One draw: the clip rectangle (left, top, right, bottom in pixels of the display), the
// texture that UiRenderer.CreateTexture gave, the first index and the count of indices in
// the index array of the frame, and the offset that the renderer adds to each index.
public readonly record struct UiDrawCommand(Vector4 ClipRectangle, ulong Texture, uint FirstIndex, uint IndexCount, int VertexOffset);

// The draw data of one frame: one array of vertices, one array of 16-bit indices, and the
// commands in their order. The display starts at DisplayPosition and has DisplaySize in
// pixels of the display; FramebufferScale gives the pixels of the target for one pixel of
// the display.
public sealed class UiDrawData(
    Vector2 displayPosition,
    Vector2 displaySize,
    Vector2 framebufferScale,
    UiVertex[] vertices,
    ushort[] indices,
    UiDrawCommand[] commands)
{
    public static readonly UiDrawData Empty = new(Vector2.Zero, Vector2.Zero, Vector2.One, [], [], []);

    public Vector2 DisplayPosition { get; } = displayPosition;

    public Vector2 DisplaySize { get; } = displaySize;

    public Vector2 FramebufferScale { get; } = framebufferScale;

    public UiVertex[] Vertices { get; } = vertices;

    public ushort[] Indices { get; } = indices;

    public UiDrawCommand[] Commands { get; } = commands;
}
