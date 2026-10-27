using System.Globalization;

namespace Alerts.Domain;

public enum DecimalKind { Price, PercentThreshold, Magnitude }

public sealed record CanonicalDecimal
{
    public decimal Value { get; }
    public string Text { get; }

    private CanonicalDecimal(decimal value)
    {
        Value = value;
        Text = Format(value);
    }

    public static CanonicalDecimal Parse(string text, DecimalKind kind)
    {
        Validate.Require(Enum.IsDefined(kind), "InvalidDecimalKind");
        Validate.Require(text is { Length: > 0 and <= 64 }, "InvalidDecimal");
        var span = text.AsSpan();
        var position = span[0] == '-' ? 1 : 0;
        Validate.Require(position < span.Length, "InvalidDecimal");
        var first = position;
        while (position < span.Length && char.IsAsciiDigit(span[position])) position++;
        Validate.Require(position > first && (position - first == 1 || span[first] != '0'), "InvalidDecimal");
        var scale = 0;
        if (position < span.Length && span[position] == '.')
        {
            var fraction = ++position;
            while (position < span.Length && char.IsAsciiDigit(span[position])) position++;
            scale = position - fraction;
            Validate.Require(scale > 0, "InvalidDecimal");
        }
        Validate.Require(position == span.Length, "InvalidDecimal");
        Validate.Require(scale <= (kind == DecimalKind.Magnitude ? 2 : 8), "DecimalScaleExceeded");
        Validate.Require(decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var value), "DecimalOverflow");
        var inRange = kind switch
        {
            DecimalKind.Price => value > 0 && value <= 1_000_000_000_000m,
            DecimalKind.PercentThreshold => value > 0 && value <= 1_000_000m,
            DecimalKind.Magnitude => value >= -2 && value <= 10,
            _ => false
        };
        Validate.Require(inRange, "DecimalOutOfRange");
        return new(value);
    }

    internal static string Format(decimal value) => value == 0 ? "0"
        : value.ToString("0.############################", CultureInfo.InvariantCulture);
}
