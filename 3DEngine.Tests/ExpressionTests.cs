using System.Globalization;
using ThreeDEngine.Core.Numeric;

namespace ThreeDEngine.Tests;

// The expression evaluator of the pad (ADR-0032 item 1, TASK-0094). ADR-0032 names this file in
// enforced-by. Each form of the grammar gives its number, and each bad form gives an error text.
public class ExpressionTests
{
    [Theory]
    [InlineData("1", 1.0)]
    [InlineData("2.5", 2.5)]
    [InlineData(".5", 0.5)]
    [InlineData("007", 7.0)]
    [InlineData(" 1 + 2 ", 3.0)]
    [InlineData("1+2*3", 7.0)]
    [InlineData("(1+2)*3", 9.0)]
    [InlineData("10-4-3", 3.0)]
    [InlineData("8/4/2", 1.0)]
    [InlineData("-3", -3.0)]
    [InlineData("--3", 3.0)]
    [InlineData("2*-3", -6.0)]
    [InlineData("-(2+3)*2", -10.0)]
    [InlineData("sqrt(16)", 4.0)]
    [InlineData("sqrt ( 9 ) + 1", 4.0)]
    [InlineData("0.1+0.2", 0.30000000000000004)]
    public void Each_Form_Of_The_Grammar_Gives_Its_Number(string text, double expected)
    {
        var result = Expression.Evaluate(text);

        Assert.True(result.IsValue, result.Error);
        Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(result.Value));
    }

    // The names give Math.PI and Math.Sqrt, with no other function.
    [Fact]
    public void Pi_And_Sqrt_Give_The_Constant_And_The_Square_Root()
    {
        Assert.Equal(Math.PI, Expression.Evaluate("pi").Value);
        Assert.Equal(2 * Math.PI, Expression.Evaluate("2*pi").Value);
        Assert.Equal(Math.Sqrt(2) * 10, Expression.Evaluate("sqrt(2)*10").Value);
    }

    [Theory]
    [InlineData("", "empty")]
    [InlineData("   ", "empty")]
    [InlineData("1+", "ends before a number")]
    [InlineData("(1+2", "closing parenthesis")]
    [InlineData("1)", "')' is not expected at character 2")]
    [InlineData("1.", "digit must follow the point")]
    [InlineData(".", "digit must follow the point")]
    [InlineData("1,5", "',' is not expected")]
    [InlineData("1 2", "'2' is not expected at character 3")]
    [InlineData("2/0", "division by zero")]
    [InlineData("2/(1-1)", "division by zero")]
    [InlineData("sqrt(-1)", "negative number")]
    [InlineData("sqrt 4", "parenthesis after it")]
    [InlineData("sq", "'sq' is not a known name")]
    [InlineData("1e3", "'e' is not expected at character 2")]
    [InlineData("e", "'e' is not a known name")]
    [InlineData("PI", "'P' is not expected")]
    [InlineData("*2", "'*' is not expected")]
    public void Each_Bad_Form_Gives_An_Error_Text(string text, string fragment)
    {
        var result = Expression.Evaluate(text);

        Assert.False(result.IsValue);
        Assert.Contains(fragment, result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void An_Expression_Has_64_Characters_At_Most()
    {
        Assert.True(Expression.Evaluate(new string('1', 64)).IsValue);
        Assert.Contains("64 characters", Expression.Evaluate(new string('1', 65)).Error, StringComparison.Ordinal);
    }

    // The point is the only decimal mark, also when the culture of the process uses a comma.
    [Fact]
    public void The_Culture_Of_The_Process_Does_Not_Change_A_Number()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            Assert.Equal(2.5, Expression.Evaluate("2.5").Value);
            Assert.False(Expression.Evaluate("2,5").IsValue);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }
}
