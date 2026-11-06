using Engine.Core;

namespace ThreeDEngine.ImageTests;

// Finding T118 of the codebase review of 2026-10-31 (c) (TASK-0150): Engine.Tests does not reference the
// desktop host, so its gate of the sinks (EventSinkContractTests, after finding E18) did not read it.
// This test project references the desktop, so it reads each sink of the desktop. A sink must not
// throw, by contract; a new sink of the desktop must enter this list with a test that it does not.
public class DesktopSinkContractTests
{
    // Phase R14e2 (TASK-0226): the sink of the local session of the phone; LocalSessionHostTests holds its test.
    private static readonly Type[] TestedSinks = [typeof(ThreeDEngine.Desktop.FanOutSink)];

    [Fact]
    public void Each_Sink_Of_The_Desktop_Has_A_Test_That_It_Does_Not_Throw()
    {
        // ADR-0072 (TASK-0222): the desktop program and its application are two assemblies.
        var sinks = new[] { typeof(ThreeDEngine.Desktop.HostedSession).Assembly, typeof(ThreeDEngine.Desktop.SessionImages).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IEventSink).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(TestedSinks, sinks);
    }
}
