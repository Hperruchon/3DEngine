namespace ThreeDEngine.ImageTests;

// The collection of the tests that use Dear ImGui (TASK-0084). The library keeps one current
// context for the whole process, so two tests that use it at the same time break each other:
// a failed check inside the native library stops the process. xunit runs the tests of one
// collection one after the other.
//
// R-0062 (TASK-0382): the collection also holds each class that creates a Vulkan device. On the computer of the owner two
// devices that the driver creates at the same time can stop the process with an access violation in vkCreateDevice, so no
// two devices are created at the same time. DeviceCollectionTests holds the rule.
[CollectionDefinition(Collection)]
public sealed class DearImGui
{
    public const string Collection = "Dear ImGui";
}
