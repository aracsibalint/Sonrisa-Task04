using System.Globalization;

namespace Alerts.Domain;

public sealed record MillisecondUtc
{
    public DateTimeOffset Value { get; }
    public long UnixMilliseconds => Value.ToUnixTimeMilliseconds();
    public string Text => Value.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    private MillisecondUtc(DateTimeOffset value) => Value = value;

    public static MillisecondUtc From(DateTimeOffset value)
    {
        Validate.Require(value.Offset == TimeSpan.Zero && value.Ticks % TimeSpan.TicksPerMillisecond == 0,
            "InvalidMillisecondUtc");
        return new(value);
    }

    public static MillisecondUtc Parse(string text)
    {
        Validate.Require(DateTimeOffset.TryParseExact(text, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var value), "InvalidMillisecondUtc");
        return From(value);
    }
}
