namespace ThreeDEngine.ImageTests;

// The collection of the tests that use Dear ImGui (TASK-0084). The library keeps one current
// context for the whole process, so two tests that use it at the same time break each other:
// a failed check inside the native library stops the process. xunit runs the tests of one
// collection one after the other.
[CollectionDefinition(Collection)]
public sealed class DearImGui
{
    public const string Collection = "Dear ImGui";
}
