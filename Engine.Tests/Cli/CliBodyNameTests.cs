using System.Text.Json;
using Engine.Core;

namespace Engine.Tests.Cli;

// The commands and the query of ADR-0028 through the command line (TASK-0085). The command
// line is one-shot (ADR-0019 item 7): each invocation has a fresh Document, so a command on a
// body gives E-GEOM-BODY-NOT-FOUND, and the rule of a name comes before the search for the
// body. The command line needs no change for a new command (ADR-0016). The chain on one
// Document is in HttpBodyNameScenarioTests.
public class CliBodyNameTests
{
    private const string SomeGuid = "11111111-1111-1111-1111-111111111111";

    private static (int Exit, string Output, string Error) Run(params string[] arguments)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = global::Engine.Cli.Cli.Run(arguments, stdout, stderr);
        return (exit, stdout.ToString(), stderr.ToString());
    }

    private static string Code(string output)
    {
        using var document = JsonDocument.Parse(output);
        return document.RootElement.GetProperty("error").GetProperty("code").GetString()!;
    }

    [Theory]
    [InlineData("apply", "RenameBody", "name=Plate")]
    [InlineData("apply", "SetBodyVisibility", "visible=false")]
    [InlineData("query", "GetBodyInfo", null)]
    public void A_Command_Or_The_Query_On_A_Fresh_Document_Finds_No_Body(string verb, string name, string? parameter)
    {
        var arguments = new List<string> { verb, name, "--param", $"bodyId={SomeGuid}" };
        if (parameter is not null)
            arguments.AddRange(["--param", parameter]);

        var (exit, output, error) = Run([.. arguments]);

        Assert.Equal(1, exit);
        Assert.Equal(string.Empty, error);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, Code(output));
    }

    [Fact]
    public void A_Name_That_Breaks_The_Rules_Is_Rejected_With_Its_Code()
    {
        var (exit, output, _) = Run("apply", "RenameBody", "--param", $"bodyId={SomeGuid}", "--param", "name=" + new string('x', 129));

        Assert.Equal(1, exit);
        Assert.Equal(DiagnosticCodes.ValBodyNameInvalid, Code(output));
    }

    // The binder refuses a visibility that is not a boolean, before the handler (ADR-0016).
    [Fact]
    public void A_Visibility_That_Is_Not_A_Boolean_Is_Refused_By_The_Binder()
    {
        var (exit, output, error) = Run("apply", "SetBodyVisibility", "--param", $"bodyId={SomeGuid}", "--param", "visible=maybe");

        Assert.Equal(2, exit);
        Assert.Equal(string.Empty, output);
        Assert.Contains("visible", error, StringComparison.Ordinal);
    }
}
