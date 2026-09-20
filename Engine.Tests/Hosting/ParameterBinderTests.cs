using System.Globalization;
using Engine.Contracts.Schema;
using Engine.Core.Hosting;
using Xunit;

namespace Engine.Tests.Hosting;

// Per ADR-0016. The binder is the one validation path for each host, therefore
// a defect here appears in the CLI and in the HTTP surface at the same time.
public class ParameterBinderTests
{
    private static Dictionary<string, FieldSchema> Schema(params (string Field, string Type, bool Required)[] fields)
    {
        var schema = new Dictionary<string, FieldSchema>(StringComparer.Ordinal);
        foreach (var (field, type, required) in fields)
            schema[field] = new FieldSchema(type, required);
        return schema;
    }

    private static Dictionary<string, object?> Raw(params (string Field, object? Value)[] values)
    {
        var raw = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (field, value) in values)
            raw[field] = value;
        return raw;
    }

    [Fact]
    public void Missing_Required_Field_Fails_And_Names_It()
    {
        var result = ParameterBinder.Bind(
            Schema(("echo", "string", true)),
            Raw());

        Assert.False(result.IsSuccess);
        Assert.Contains("echo", result.FirstMessage);
    }

    [Fact]
    public void Missing_Optional_Field_Succeeds_And_Is_Absent()
    {
        var result = ParameterBinder.Bind(
            Schema(("note", "string", false)),
            Raw());

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("note", result.Values!.Keys);
    }

    [Fact]
    public void Unknown_Field_Fails()
    {
        // A silent discard would hide a typo in a script and in an agent request.
        var result = ParameterBinder.Bind(
            Schema(("echo", "string", true)),
            Raw(("echo", "hello"), ("eco", "hello")));

        Assert.False(result.IsSuccess);
        Assert.Contains("eco", result.Failures[0].Field + result.Failures[0].Message);
    }

    [Theory]
    [InlineData("number", "abc")]
    [InlineData("integer", "1.5")]
    [InlineData("boolean", "yes")]
    [InlineData("guid", "not-a-guid")]
    public void Wrong_Type_Fails_And_Names_The_Field_And_The_Value(string type, string supplied)
    {
        var result = ParameterBinder.Bind(
            Schema(("field", type, true)),
            Raw(("field", supplied)));

        Assert.False(result.IsSuccess);
        Assert.Contains("field", result.FirstMessage);
        Assert.Contains(supplied, result.FirstMessage);
    }

    [Fact]
    public void A_String_Parses_To_Each_Declared_Type()
    {
        var id = Guid.NewGuid();
        var result = ParameterBinder.Bind(
            Schema(("n", "number", true), ("i", "integer", true),
                   ("b", "boolean", true), ("g", "guid", true), ("s", "string", true)),
            Raw(("n", "1.5"), ("i", "42"), ("b", "true"), ("g", id.ToString()), ("s", "text")));

        Assert.True(result.IsSuccess, result.FirstMessage);
        Assert.Equal(1.5d, result.Values!["n"]);
        Assert.Equal(42L, result.Values["i"]);
        Assert.Equal(true, result.Values["b"]);
        Assert.Equal(id, result.Values["g"]);
        Assert.Equal("text", result.Values["s"]);
    }

    [Fact]
    public void An_Already_Typed_Value_Passes_Through_Unchanged()
    {
        // The HTTP surface supplies a double from the JSON reader. It must not
        // pass through a string, because a string round-trip can lose precision.
        const double exact = 0.1 + 0.2;
        var result = ParameterBinder.Bind(
            Schema(("n", "number", true)),
            Raw(("n", exact)));

        Assert.True(result.IsSuccess);
        Assert.Equal(exact, (double)result.Values!["n"]!);
    }

    [Fact]
    public void A_Number_Parses_With_The_Invariant_Culture()
    {
        // CLAUDE.md "Determinism rules": a culture-sensitive parse makes the
        // same input produce different numbers on a machine with a comma
        // decimal separator, and the log would then not denote the model.
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

            var result = ParameterBinder.Bind(
                Schema(("n", "number", true)),
                Raw(("n", "1.5")));

            Assert.True(result.IsSuccess, result.FirstMessage);
            Assert.Equal(1.5d, (double)result.Values!["n"]!);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_Comma_Decimal_Is_Refused_Under_A_Comma_Culture()
    {
        // The other half of the same rule: "1,5" must not become 1.5 because
        // the machine happens to use a comma.
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

            var result = ParameterBinder.Bind(
                Schema(("n", "number", true)),
                Raw(("n", "1,5")));

            Assert.False(result.IsSuccess);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_Deferred_Declared_Type_Fails_Rather_Than_Passes_Silently()
    {
        // ADR-0016 defers "object" and "array". A silent acceptance would hide
        // the decision to extend the vocabulary.
        var result = ParameterBinder.Bind(
            Schema(("shape", "object", true)),
            Raw(("shape", "{}")));

        Assert.False(result.IsSuccess);
        Assert.Contains("object", result.FirstMessage);
    }

    [Fact]
    public void Each_Failure_Is_Reported_Not_Only_The_First()
    {
        var result = ParameterBinder.Bind(
            Schema(("a", "number", true), ("b", "guid", true)),
            Raw(("a", "x"), ("b", "y")));

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Failures.Count);
    }
}
