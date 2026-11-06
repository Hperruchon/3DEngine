using System.Text.RegularExpressions;
using Xunit;

namespace Engine.Tests.Governance;

// Finding T163 of the codebase review of 2026-11-06 (e) (TASK-0251): two of the three patterns of a native crash in
// eng/android/smoke-emulator.sh could not match the line that Android writes, because the line of libc gives the name of
// the main thread, the last 15 characters of the package, and TASK-0242 proved the patterns with a line that Android does
// not write. The patterns are now the lines of eng/android/native-crash.ere, which the script gives to grep -E -f. These
// tests use the forms of the lines of debuggerd: "Fatal signal" of libc, and the lines "pid: ... >>> name <<<" and
// "Cmdline:" of the tombstone of DEBUG, with the prefix of logcat -d (the format threadtime).
public sealed class NativeCrashPatternTests
{
    private static Regex[] Patterns()
        => File.ReadAllLines(RepositoryFiles.Path("eng", "android", "native-crash.ere"))
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0)
            .Select(line => new Regex(line, RegexOptions.CultureInvariant))
            .ToArray();

    [Theory]
    [InlineData("11-06 22:00:01.123  3410  3426 F libc    : Fatal signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr 0x0 in tid 3426 (SDLThread), pid 3410 (eedengine.phone)")]
    [InlineData("11-06 22:00:01.123  3410  3426 F libc    : Fatal signal 6 (SIGABRT), code -1 (SI_QUEUE) in tid 3426 (SDLThread), pid 3410 (org.threedengine.phone)")]
    [InlineData("11-06 22:00:01.456  3440  3440 F DEBUG   : pid: 3410, tid: 3426, name: SDLThread  >>> org.threedengine.phone <<<")]
    [InlineData("11-06 22:00:01.456  3440  3440 F DEBUG   : Cmdline: org.threedengine.phone")]
    public void A_Line_Of_A_Native_Crash_Of_The_Program_Matches(string line)
        => Assert.Contains(Patterns(), pattern => pattern.IsMatch(line));

    // The controls: a crash of another process, and the lines of a start, match no pattern.
    [Theory]
    [InlineData("11-06 22:00:01.123  2210  2230 F libc    : Fatal signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr 0x0 in tid 2230 (RenderThread), pid 2210 (com.android.systemui)")]
    [InlineData("11-06 22:00:01.456  3440  3440 F DEBUG   : pid: 2210, tid: 2230, name: RenderThread  >>> com.android.systemui <<<")]
    [InlineData("11-06 22:00:01.456  3440  3440 F DEBUG   : Cmdline: com.android.systemui")]
    [InlineData("11-06 22:00:00.100  3410  3426 I DOTNET  : 3DEngine: the session of the phone starts")]
    [InlineData("11-06 22:00:00.050   612   640 I ActivityManager: Start proc 3410:org.threedengine.phone/u0a190 for next-top-activity")]
    public void Another_Line_Matches_No_Pattern(string line)
        => Assert.DoesNotContain(Patterns(), pattern => pattern.IsMatch(line));

    // The script reads the file of the patterns; until TASK-0251 it held the patterns in its own text.
    [Fact]
    public void The_Smoke_Test_Reads_The_File_Of_The_Patterns()
    {
        var script = RepositoryFiles.Read("eng", "android", "smoke-emulator.sh");
        Assert.Contains("native-crash.ere", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Fatal signal .*", script, StringComparison.Ordinal);
    }
}
