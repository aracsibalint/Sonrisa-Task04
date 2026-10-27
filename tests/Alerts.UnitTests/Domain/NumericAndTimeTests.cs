using Alerts.Domain;
using Xunit;

namespace Alerts.UnitTests.Domain;

public sealed class NumericAndTimeTests
{
    [Theory]
    [InlineData("1.23000000", DecimalKind.Price, "1.23")]
    [InlineData("1000000000000", DecimalKind.Price, "1000000000000")]
    [InlineData("0.00000001", DecimalKind.Price, "0.00000001")]
    [InlineData("1000000", DecimalKind.PercentThreshold, "1000000")]
    [InlineData("-0.00", DecimalKind.Magnitude, "0")]
    [InlineData("-2.00", DecimalKind.Magnitude, "-2")]
    [InlineData("10.00", DecimalKind.Magnitude, "10")]
    public void Parse_ValidDecimal_ProducesCanonicalText(string input, DecimalKind kind, string expected)
        => Assert.Equal(expected, CanonicalDecimal.Parse(input, kind).Text);

    [Theory]
    [InlineData("", DecimalKind.Price)]
    [InlineData("01", DecimalKind.Price)]
    [InlineData("+1", DecimalKind.Price)]
    [InlineData(" 1", DecimalKind.Price)]
    [InlineData("1 ", DecimalKind.Price)]
    [InlineData("1e2", DecimalKind.Price)]
    [InlineData("1,2", DecimalKind.Price)]
    [InlineData("NaN", DecimalKind.Price)]
    [InlineData("Infinity", DecimalKind.Price)]
    [InlineData("١", DecimalKind.Price)]
    [InlineData(".1", DecimalKind.Price)]
    [InlineData("1.", DecimalKind.Price)]
    [InlineData("--1", DecimalKind.Price)]
    [InlineData("1.000000000", DecimalKind.Price)]
    [InlineData("0", DecimalKind.Price)]
    [InlineData("-1", DecimalKind.Price)]
    [InlineData("1000000000000.00000001", DecimalKind.Price)]
    [InlineData("79228162514264337593543950336", DecimalKind.Price)]
    [InlineData("0", DecimalKind.PercentThreshold)]
    [InlineData("-1", DecimalKind.PercentThreshold)]
    [InlineData("1000000.00000001", DecimalKind.PercentThreshold)]
    [InlineData("-2.01", DecimalKind.Magnitude)]
    [InlineData("10.01", DecimalKind.Magnitude)]
    [InlineData("1.000", DecimalKind.Magnitude)]
    public void Parse_InvalidDecimal_RejectsWithoutEchoingInput(string input, DecimalKind kind)
    {
        var error = Assert.Throws<DomainValidationException>(() => CanonicalDecimal.Parse(input, kind));
        Assert.Contains(error.Code, new[] { "InvalidDecimal", "DecimalScaleExceeded", "DecimalOverflow", "DecimalOutOfRange" });
        Assert.Equal(error.Code, error.Message);
    }

    [Theory]
    [InlineData("2026-01-01T00:00:00Z")]
    [InlineData("2026-01-01T00:00:00.0001Z")]
    [InlineData("2026-01-01T00:00:00.000+00:00")]
    [InlineData("2026-01-01T01:00:00.000+01:00")]
    [InlineData("2026-02-30T00:00:00.000Z")]
    public void Parse_InvalidTimestamp_RejectsPrecisionOffsetAndDate(string input)
        => Assert.Equal("InvalidMillisecondUtc", Assert.Throws<DomainValidationException>(() => MillisecondUtc.Parse(input)).Code);

    [Fact]
    public void From_NonUtcOrSubMillisecond_RejectsInsteadOfRounding()
    {
        Assert.Throws<DomainValidationException>(() => MillisecondUtc.From(DateTimeOffset.UnixEpoch.AddTicks(1)));
        Assert.Throws<DomainValidationException>(() => MillisecondUtc.From(DateTimeOffset.UnixEpoch.ToOffset(TimeSpan.FromHours(1))));
        Assert.Equal("1970-01-01T00:00:00.000Z", MillisecondUtc.From(DateTimeOffset.UnixEpoch).Text);
        Assert.Equal(0, MillisecondUtc.Parse("1970-01-01T00:00:00.000Z").UnixMilliseconds);
    }
}
