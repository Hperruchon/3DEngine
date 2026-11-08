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

    // Finding V141 of the codebase review of 2026-11-08 (TASK-0294): run 42433201851 gave the line Cmdline of the tombstone
    // only. The lines of eng/android/tombstone-lines.ere give the thread, the signal, the abort message and the first frames
    // of the stack, in the forms of debuggerd with the prefix of logcat -d.
    private static Regex[] TombstonePatterns()
        => File.ReadAllLines(RepositoryFiles.Path("eng", "android", "tombstone-lines.ere"))
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0)
            .Select(line => new Regex(line, RegexOptions.CultureInvariant))
            .ToArray();

    [Theory]
    [InlineData("11-08 01:16:39.044  4860  4860 F DEBUG   : pid: 3741, tid: 3927, name: SDLThread  >>> org.threedengine.phone <<<")]
    [InlineData("11-08 01:16:39.044  4860  4860 F DEBUG   : signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr 0x0000000000000010")]
    [InlineData("11-08 01:16:39.044  4860  4860 F DEBUG   : Abort message: 'FORTIFY: pthread_mutex_lock called on a destroyed mutex'")]
    [InlineData("11-08 01:16:39.044  4860  4860 F DEBUG   :       #00 pc 000000000005e1b0  /data/app/~~a/org.threedengine.phone-b/lib/x86_64/libSDL3.so (BuildId: 0123)")]
    [InlineData("11-08 01:16:39.044  4860  4860 F DEBUG   :       #05 pc 00000000000b2c40  /apex/com.android.runtime/lib64/bionic/libc.so (__start_thread+64)")]
    public void A_Line_Of_A_Tombstone_That_Names_The_Cause_Matches(string line)
        => Assert.Contains(TombstonePatterns(), pattern => pattern.IsMatch(line));

    // The controls: the lines of the tombstone that name no cause, a frame after the sixth, and a line of the program.
    [Theory]
    [InlineData("11-08 01:16:39.044  4860  4860 F DEBUG   : Build fingerprint: 'google/sdk_gphone64_x86_64/emu64xa:14/UE1A/1:userdebug/dev-keys'")]
    [InlineData("11-08 01:16:39.044  4860  4860 F DEBUG   :     rax 0000000000000000  rbx 00007b1c8e2f4000  rcx 0000000000000000")]
    [InlineData("11-08 01:16:39.044  4860  4860 F DEBUG   :       #06 pc 0000000000052a10  /apex/com.android.runtime/lib64/bionic/libc.so")]
    [InlineData("11-08 01:16:38.238  3741  3927 I DOTNET  : 3DEngine: the surface of the window is lost while the engine starts")]
    public void A_Line_That_Names_No_Cause_Matches_No_Tombstone_Pattern(string line)
        => Assert.DoesNotContain(TombstonePatterns(), pattern => pattern.IsMatch(line));

    [Fact]
    public void The_Smoke_Test_Gives_The_Lines_Of_The_Tombstone()
        => Assert.Contains("tombstone-lines.ere", RepositoryFiles.Read("eng", "android", "smoke-emulator.sh"), StringComparison.Ordinal);
}
