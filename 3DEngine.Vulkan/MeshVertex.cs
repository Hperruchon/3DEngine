using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The vertex format of a mesh (roadmap phase R3, TASK-0069): one position of three
// 32-bit floats, as the shader mesh.vert reads it. The engine gives each position
// in double precision (ADR-0018 item 3); the narrowing to float is presentation and
// happens here.
public static class MeshVertex
{
    public const uint Stride = 3 * sizeof(float);

    public static VkVertexInputBindingDescription Binding => new()
    {
        binding = 0,
        stride = Stride,
        inputRate = VkVertexInputRate.Vertex,
    };

    public static VkVertexInputAttributeDescription Position => new()
    {
        location = 0,
        binding = 0,
        format = VkFormat.R32G32B32Sfloat,
        offset = 0,
    };

    // Each position minus the origin, times the scale, narrowed to float. The
    // subtraction is in double precision, before the narrowing, so a body far from
    // the origin of the engine keeps its detail (roadmap phase R5).
    public static float[] Narrow(ReadOnlySpan<double> positions, (double X, double Y, double Z) origin, double scale)
    {
        if (positions.Length % 3 != 0)
            throw new ArgumentException("A position has three values.", nameof(positions));

        var result = new float[positions.Length];
        for (var i = 0; i < positions.Length; i += 3)
        {
            result[i] = (float)((positions[i] - origin.X) * scale);
            result[i + 1] = (float)((positions[i + 1] - origin.Y) * scale);
            result[i + 2] = (float)((positions[i + 2] - origin.Z) * scale);
        }

        return result;
    }
}
