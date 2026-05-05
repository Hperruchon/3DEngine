namespace Engine.Tests.Cli;

public class CliUsageTests
{
    [Fact]
    public void NoArgs_Returns_Exit2_And_Prints_Usage_To_Stderr()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(Array.Empty<string>(), stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Usage:", stderr.ToString());
        Assert.Contains("engine apply", stderr.ToString());
        Assert.Contains("engine query", stderr.ToString());
    }

    [Fact]
    public void Help_Verb_Returns_Exit0_And_Prints_Usage_To_Stdout()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(new[] { "help" }, stdout, stderr);

        Assert.Equal(0, exit);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.Contains("Usage:", stdout.ToString());
    }

    [Fact]
    public void Unknown_Verb_Returns_Exit2_And_Prints_Usage_To_Stderr()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run(new[] { "wibble" }, stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Unknown verb: wibble", stderr.ToString());
        Assert.Contains("Usage:", stderr.ToString());
    }
}
