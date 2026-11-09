using System.Diagnostics;
using System.Text;

namespace Engine.Tests.Cli;

// Finding E17 of the codebase review of 2026-09-30 (R-0028, TASK-0321): the command line wrote text that is
// not ASCII in the code page of the console, so a script that reads UTF-8 got other characters. The
// program writes UTF-8.
public class CliEncodingTests
{
    [Fact]
    public async Task The_Program_Writes_Text_That_Is_Not_Ascii_As_Utf8()
    {
        // The program and its runtime configuration are in the test output, because Engine.Tests
        // references Engine.Cli.
        var dll = Path.Combine(AppContext.BaseDirectory, "engine.dll");
        Assert.True(File.Exists(dll), $"{dll} is absent.");

        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(dll);
        start.ArgumentList.Add("Größe-Ω");

        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);

        Assert.Equal(2, process.ExitCode);
        Assert.Contains("Unknown verb: Größe-Ω", await error, StringComparison.Ordinal);
        Assert.Equal(string.Empty, await output);
    }
}
