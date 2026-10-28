namespace ThreeDEngine.Core.Numeric;

// One field of the pad: its name, for example "dx", and the text of its expression.
public sealed record PadField(string Name, string Text);

// The state machine of the numeric pad (ADR-0032 item 3, TASK-0094). It holds three fields and
// the active field. A button appends the text of its label, and a typed character appends the
// same text, so a tap, a click and the keyboard give the same states. The pad is a state of the
// client (ADR-0007): it gives numbers, and the host makes the command.
public sealed class NumericPad
{
    // The characters of the grammar of Expression; the pad drops each other character.
    public const string Characters = "0123456789.+-*/() pisqrt";

    private readonly string[] _texts;
    private readonly string[] _names;

    public NumericPad(string title, string unit, params PadField[] fields)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(unit);
        if (fields is not { Length: 3 })
            throw new ArgumentException("A pad has three fields.", nameof(fields));

        Title = title;
        Unit = unit;
        _names = fields.Select(field => field.Name).ToArray();
        _texts = fields.Select(field => field.Text).ToArray();
    }

    public string Title { get; }

    // The length unit of the document, which the pad shows beside each field (ADR-0032 item 7).
    public string Unit { get; }

    public IReadOnlyList<PadField> Fields => _names.Select((name, index) => new PadField(name, _texts[index])).ToArray();

    // The index of the field that the next text goes to.
    public int Active { get; private set; }

    // The error of the last apply, or the rejection of its command, or null (ADR-0032 item 8).
    public string? Message { get; private set; }

    // The value or the error of the active field, as the pad shows it while the person types.
    public ExpressionResult Preview => Expression.Evaluate(_texts[Active]);

    public void Append(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var accepted = new string(text.Where(Characters.Contains).ToArray());
        var room = Expression.MaximumLength - _texts[Active].Length;
        _texts[Active] += accepted[..Math.Min(accepted.Length, Math.Max(room, 0))];
        Message = null;
    }

    public void Delete()
    {
        if (_texts[Active].Length > 0)
            _texts[Active] = _texts[Active][..^1];
        Message = null;
    }

    public void Clear()
    {
        _texts[Active] = string.Empty;
        Message = null;
    }

    public void Next() => Active = (Active + 1) % _texts.Length;

    public void Select(int field)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(field);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(field, _texts.Length);
        Active = field;
    }

    // The three numbers, or null with the error of the first field that fails in Message. A
    // field that fails becomes the active field, so the next text corrects it.
    public double[]? Apply()
    {
        var values = new double[_texts.Length];
        for (var index = 0; index < _texts.Length; index++)
        {
            var result = Expression.Evaluate(_texts[index]);
            if (!result.IsValue)
            {
                Active = index;
                Message = $"{_names[index]}: {result.Error}";
                return null;
            }

            values[index] = result.Value;
        }

        Message = null;
        return values;
    }

    // The host gives the rejection of the command of an apply.
    public void Fail(string message) => Message = message ?? throw new ArgumentNullException(nameof(message));
}
