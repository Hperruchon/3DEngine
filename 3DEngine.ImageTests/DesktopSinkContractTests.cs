using Engine.Core;

namespace ThreeDEngine.ImageTests;

// Finding T118 of the codebase review of 2026-10-31 (c) (TASK-0150): Engine.Tests does not reference the
// desktop host, so its gate of the sinks (EventSinkContractTests, after finding E18) did not read it.
// This test project references the desktop, so it reads each sink of the desktop. A sink must not
// throw, by contract; a new sink of the desktop must enter this list with a test that it does not.
public class DesktopSinkContractTests
{
    private static readonly Type[] TestedSinks = [];

    [Fact]
    public void Each_Sink_Of_The_Desktop_Has_A_Test_That_It_Does_Not_Throw()
    {
        var sinks = typeof(ThreeDEngine.Desktop.SessionImages).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IEventSink).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(TestedSinks, sinks);
    }
}
