using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The choice of a memory type (TASK-0069; ADR-0017 said that R3 writes this helper
// again). It is apart from the device so that a test can call it with no GPU.
public static class MemoryTypes
{
    // The index of the first memory type that the bits of a resource permit and
    // that has each wanted property, or -1.
    public static int Find(uint permittedTypeBits, ReadOnlySpan<VkMemoryPropertyFlags> typeProperties, VkMemoryPropertyFlags wanted)
    {
        for (var index = 0; index < typeProperties.Length && index < 32; index++)
        {
            if ((permittedTypeBits & (1u << index)) != 0 && (typeProperties[index] & wanted) == wanted)
                return index;
        }

        return -1;
    }
}
