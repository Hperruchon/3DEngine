using System.Globalization;

namespace ThreeDEngine.Core.Numeric;

// The result of an evaluation: a finite number, or a short text that tells the person what is
// wrong (ADR-0032 items 1 and 8).
public readonly record struct ExpressionResult(double Value, string? Error)
{
    public bool IsValue => Error is null;

    public static ExpressionResult Of(double value) => new(value, null);

    public static ExpressionResult Failed(string error) => new(double.NaN, error);
}

// The expression evaluator of the numeric pad (ADR-0032 item 1, TASK-0094). A recursive descent
// over this grammar, with a space permitted between two tokens:
//
//   expression = term { ("+" | "-") term }
//   term       = unary { ("*" | "/") unary }
//   unary      = "-" unary | primary
//   primary    = number | "pi" | "sqrt" "(" expression ")" | "(" expression ")"
//   number     = digits [ "." digits ] | "." digits
//
// It uses only + - * /, Math.Sqrt and the constant Math.PI, and it reads each number with the
// invariant culture, so the point is the only decimal mark. The client evaluates; a command
// records the result number only, so a replay never evaluates (ADR-0032 item 2, ADR-0007).
public static class Expression
{
    public const int MaximumLength = 64;

    public static ExpressionResult Evaluate(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > MaximumLength)
            return ExpressionResult.Failed($"An expression has {MaximumLength} characters at most.");

        try
        {
            var value = new Parser(text).Whole();
            return double.IsFinite(value) ? ExpressionResult.Of(value) : ExpressionResult.Failed("The result is not a finite number.");
        }
        catch (ExpressionException failure)
        {
            return ExpressionResult.Failed(failure.Message);
        }
    }

    private sealed class ExpressionException(string message) : Exception(message);

    private sealed class Parser(string text)
    {
        private int _at;

        public double Whole()
        {
            Skip();
            if (_at == text.Length)
                throw new ExpressionException("The expression is empty.");
            var value = Sum();
            Skip();
            if (_at < text.Length)
                throw Unexpected();
            return value;
        }

        private double Sum()
        {
            var value = Product();
            while (true)
            {
                Skip();
                if (Take('+'))
                    value += Product();
                else if (Take('-'))
                    value -= Product();
                else
                    return value;
            }
        }

        private double Product()
        {
            var value = Unary();
            while (true)
            {
                Skip();
                if (Take('*'))
                    value *= Unary();
                else if (Take('/'))
                {
                    var divisor = Unary();
                    if (divisor == 0)
                        throw new ExpressionException("A division by zero has no value.");
                    value /= divisor;
                }
                else
                    return value;
            }
        }

        private double Unary()
        {
            Skip();
            return Take('-') ? -Unary() : Primary();
        }

        private double Primary()
        {
            Skip();
            if (_at == text.Length)
                throw new ExpressionException("The expression ends before a number.");

            if (Take('('))
                return Closed(Sum());

            if (char.IsAsciiLetterLower(text[_at]))
            {
                var start = _at;
                while (_at < text.Length && char.IsAsciiLetterLower(text[_at]))
                    _at++;
                switch (text[start.._at])
                {
                    case "pi":
                        return Math.PI;
                    case "sqrt":
                        Skip();
                        if (!Take('('))
                            throw new ExpressionException("sqrt needs a parenthesis after it.");
                        var radicand = Closed(Sum());
                        if (radicand < 0)
                            throw new ExpressionException("The square root of a negative number has no value.");
                        return Math.Sqrt(radicand);
                    default:
                        throw new ExpressionException($"'{text[start.._at]}' is not a known name. The names are pi and sqrt.");
                }
            }

            return Number();
        }

        private double Closed(double value)
        {
            Skip();
            if (!Take(')'))
                throw new ExpressionException("A closing parenthesis is missing.");
            return value;
        }

        private double Number()
        {
            var start = _at;
            var whole = Digits();
            if (Take('.') && Digits() == 0)
                throw new ExpressionException("A digit must follow the point.");
            if (whole == 0 && _at == start)
                throw Unexpected();
            return double.Parse(text.AsSpan(start, _at - start), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        private int Digits()
        {
            var start = _at;
            while (_at < text.Length && char.IsAsciiDigit(text[_at]))
                _at++;
            return _at - start;
        }

        private bool Take(char expected)
        {
            if (_at < text.Length && text[_at] == expected)
            {
                _at++;
                return true;
            }

            return false;
        }

        private void Skip()
        {
            while (_at < text.Length && text[_at] == ' ')
                _at++;
        }

        private ExpressionException Unexpected() => new($"'{text[_at]}' is not expected at character {_at + 1}.");
    }
}
