using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.Tests;

// The vertex format of a mesh and the choice of a memory type (TASK-0069), with no
// GPU.
public class MeshVertexTests
{
    [Fact]
    public void A_Vertex_Is_One_Position_Of_Three_Floats_At_Binding_0_And_Location_0()
    {
        Assert.Equal(12u, MeshVertex.Stride);
        Assert.Equal(0u, MeshVertex.Binding.binding);
        Assert.Equal(12u, MeshVertex.Binding.stride);
        Assert.Equal(VkVertexInputRate.Vertex, MeshVertex.Binding.inputRate);
        Assert.Equal(0u, MeshVertex.Position.location);
        Assert.Equal(0u, MeshVertex.Position.binding);
        Assert.Equal(VkFormat.R32G32B32Sfloat, MeshVertex.Position.format);
        Assert.Equal(0u, MeshVertex.Position.offset);
    }

    // The subtraction happens in double precision before the narrowing. A float
    // holds no fraction at 1e9, so a narrowing before the subtraction gives 0.
    [Fact]
    public void The_Origin_Is_Subtracted_Before_The_Narrowing_To_Float()
    {
        var floats = MeshVertex.Narrow([1e9 + 0.25, -2, 3], (1e9, 0, 0), 2);

        Assert.Equal([0.5f, -4f, 6f], floats);
        Assert.Equal(0f, (float)(1e9 + 0.25) - (float)1e9);
    }

    [Fact]
    public void A_List_Of_Positions_That_Is_Not_A_Multiple_Of_Three_Is_Refused()
    {
        Assert.Throws<ArgumentException>(() => MeshVertex.Narrow([1, 2], (0, 0, 0), 1));
    }

    [Theory]
    [InlineData(0b111u, 1)]
    [InlineData(0b101u, 2)]
    [InlineData(0b001u, -1)]
    public void A_Memory_Type_Must_Be_Permitted_And_Have_Each_Wanted_Property(uint permitted, int expected)
    {
        VkMemoryPropertyFlags[] types =
        [
            VkMemoryPropertyFlags.DeviceLocal,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
            VkMemoryPropertyFlags.DeviceLocal | VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
        ];

        Assert.Equal(expected, MemoryTypes.Find(permitted, types, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent));
    }
}
