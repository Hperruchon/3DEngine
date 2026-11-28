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

    // Finding E17 of the codebase review of 2026-09-30, part 9 (register entry R-0028, TASK-0481): an option in the place of the
    // name of a command or of a query is a usage error with the exit code 2, and nothing goes to the standard output. Until
    // TASK-0481 the option was the name, so the result of an unknown command or query went to the standard output with exit 1.
    [Theory]
    [InlineData("apply", "--kind", "part", "CreateBox")]
    [InlineData("apply", "--folder", "x", "CreateBox")]
    [InlineData("query", "--kind", "part", "GetBodies")]
    public void An_Option_In_The_Place_Of_The_Name_Is_A_Usage_Error(string verb, string option, string value, string name)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = global::Engine.Cli.Cli.Run([verb, option, value, name], stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains($"'{option}' is an option", stderr.ToString(), StringComparison.Ordinal);
        Assert.Contains("Usage:", stderr.ToString());
    }
}
