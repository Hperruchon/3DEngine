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

    // The centre of the box of the positions, in double precision; a host narrows a
    // mesh around it and places the mesh at it (ADR-0024 item 2).
    public static (double X, double Y, double Z) Centre(ReadOnlySpan<double> positions)
    {
        if (positions.Length == 0 || positions.Length % 3 != 0)
            throw new ArgumentException("A mesh has at least one position of three values.", nameof(positions));

        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
        for (var i = 0; i < positions.Length; i += 3)
        {
            (minX, maxX) = (Math.Min(minX, positions[i]), Math.Max(maxX, positions[i]));
            (minY, maxY) = (Math.Min(minY, positions[i + 1]), Math.Max(maxY, positions[i + 1]));
            (minZ, maxZ) = (Math.Min(minZ, positions[i + 2]), Math.Max(maxZ, positions[i + 2]));
        }

        return ((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
    }

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
