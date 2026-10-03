namespace Engine.Tests;

// An injected failure for register entry R-0032. This branch is never merged.
public class InjectedFailureTests
{
    [Fact]
    public void Injected_Failure_For_R0032() => Assert.Fail("Injected: the pipeline must name this test.");
}
