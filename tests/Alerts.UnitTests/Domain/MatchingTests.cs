using System.Globalization;
using Alerts.Domain;
using Xunit;

namespace Alerts.UnitTests.Domain;

public sealed class MatchingTests
{
    internal static readonly MillisecondUtc End = MillisecondUtc.Parse("2026-01-01T00:00:00.000Z");
    internal static readonly MillisecondUtc Start = MillisecondUtc.Parse("2025-12-31T00:00:00.000Z");
    internal static MarketPayload Market(string last, string open = "100")
        => new("BTCUSDT", "USDT", "binance-rolling-24h", open, last, Start, End);

    [Theory]
    [InlineData("105", "5", true)]
    [InlineData("95", "5", true)]
    [InlineData("104.999", "5", false)]
    [InlineData("95.001", "5", false)]
    [InlineData("100", "5", false)]
    [InlineData("105", "5.00000001", false)]
    public void Matches_Market_UsesAbsoluteInclusiveUnroundedCrossProduct(string last, string threshold, bool expected)
        => Assert.Equal(expected, MatchPredicates.Matches(new MarketFilter(threshold), Market(last)));

    [Theory]
    [InlineData("999999999999.99999999", "100", false)]
    [InlineData("999999999999.99999999", "99.99999999", true)]
    [InlineData("999999999999.99999999", "100.00000001", false)]
    [InlineData("499999999999.99999999", "999999999949.99999998", false)]
    [InlineData("499999999999.99999999", "999999999949.99999999", true)]
    [InlineData("499999999999.99999999", "50", false)]
    [InlineData("499999999999.99999999", "49.99999999", true)]
    [InlineData("500000000000", "50", true)]
    [InlineData("500000000000", "50.00000001", false)]
    [InlineData("500000000000", "999999999950", true)]
    [InlineData("500000000000", "999999999949.99999999", false)]
    public void Matches_MarketNearEquality_UsesExactProductsBeyondDecimalPrecision(string open, string last, bool expected)
        => Assert.Equal(expected, MatchPredicates.Matches(new MarketFilter("99.99999999"), Market(last, open)));

    [Fact]
    public void Matches_MaximumPricesAndThreshold_RemainsExactAndBounded()
    {
        Assert.False(MatchPredicates.Matches(new MarketFilter("1000000"), Market("1000000000000", "999999999999")));
        Assert.True(MatchPredicates.Matches(new MarketFilter("1000000"), Market("1000000000000", "0.00000001")));
        Assert.Equal(-5, Market("95").SignedMovementPercent);
        Assert.True(MatchPredicates.Matches(new MarketFilter("5"), Market("105")));
        Assert.True(MatchPredicates.Matches(new MarketFilter("5"), Market("105"))); // No crossing/cooldown state.
    }

    [Theory]
    [InlineData("4.5", "4.5", true)]
    [InlineData("4.49", "4.5", false)]
    [InlineData("-2", "-2", true)]
    [InlineData("-1.99", "-1.98", false)]
    [InlineData("10", "10", true)]
    public void Matches_Earthquake_UsesInclusiveMw(string magnitude, string minimum, bool expected)
        => Assert.Equal(expected, MatchPredicates.Matches(new EarthquakeFilter(minimum), new EarthquakePayload(magnitude, "Mw", true)));

    [Theory]
    [InlineData("moon", "Moon landing", null, true)]
    [InlineData("moon", "Other news", "The MOON is bright", true)]
    [InlineData("café", "Cafe\u0301 launch", null, true)]
    [InlineData("moon\t  landing", "Moon\u00a0\u00a0landing", null, true)]
    [InlineData("moon", "mo", "on", false)]
    [InlineData("mo on", "mo", "on", true)]
    [InlineData(".*", "All news", null, false)]
    [InlineData(".*", "Literal .* here", null, true)]
    [InlineData("other", "Moon", null, false)]
    public void Matches_News_NormalizesPlainTextAndUsesLiteralSubstring(string keyword, string title, string? summary, bool expected)
        => Assert.Equal(expected, MatchPredicates.Matches(new NewsFilter(keyword), new NewsPayload(title, summary)));

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("hu-HU")]
    public void MatchingAndDecimals_DifferentCulture_StayOrdinalAndInvariant(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Assert.True(MatchPredicates.Matches(new NewsFilter("FILE"), new NewsPayload("file")));
            Assert.False(MatchPredicates.Matches(new NewsFilter("I"), new NewsPayload("ı")));
            Assert.Equal("1.5", CanonicalDecimal.Parse("1.50", DecimalKind.Price).Text);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void Text_LengthsUnicodeAndByteBounds_AreEnforced()
    {
        Assert.Equal(100, new NewsFilter(new string('a', 100)).Keyword.Length);
        Assert.Throws<DomainValidationException>(() => new NewsFilter(new string('a', 101)));
        Assert.Throws<DomainValidationException>(() => new NewsFilter(" \t "));
        Assert.Throws<DomainValidationException>(() => new NewsPayload("bad\ntitle"));
        Assert.Throws<DomainValidationException>(() => new NewsPayload("bad\ud800"));
        Assert.Throws<DomainValidationException>(() => new NewsPayload("title", articleUrl: "https://example.test/" + new string('\ud800', 1)));
        Assert.Throws<DomainValidationException>(() => new NewsPayload(new string('a', 501)));
        Assert.Throws<DomainValidationException>(() => new NewsPayload("title", new string('a', 4001)));
        Assert.Equal(4000, new NewsPayload(new string('a', 500), new string('a', 4000)).Summary!.Length);
        var emojiTitle = string.Concat(Enumerable.Repeat("😀", 500));
        var emojiSummary = string.Concat(Enumerable.Repeat("😀", 4000));
        Assert.Throws<DomainValidationException>(() => new NewsPayload(emojiTitle, emojiSummary));
        Assert.Throws<DomainValidationException>(() => new NewsPayload("title", "", "https://example.test"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:password@example.test/a")]
    [InlineData("https://example.test/a\r\nInjected: value")]
    [InlineData("https://example.test\\ambiguous")]
    public void News_UnsafeNavigationUrl_Rejects(string url)
        => Assert.Throws<DomainValidationException>(() => new NewsPayload("title", articleUrl: url));

    [Fact]
    public void Payloads_UnsupportedUnitsWindowsAndSimulation_Reject()
    {
        Assert.Throws<DomainValidationException>(() => new MarketPayload("BTCUSD", "USD", "binance-rolling-24h", "100", "105", Start, End));
        Assert.Throws<DomainValidationException>(() => new MarketPayload("BTCUSDT", "USDT", "other", "100", "105", Start, End));
        Assert.Throws<DomainValidationException>(() => new MarketPayload("BTCUSDT", "USDT", "binance-rolling-24h", "100", "105", End, Start));
        Assert.Throws<DomainValidationException>(() => new EarthquakePayload("4", "Richter", true));
        Assert.Throws<DomainValidationException>(() => new EarthquakePayload("4", "Mw", false));
        Assert.Throws<DomainValidationException>(() => new EarthquakeFilter("4", "Richter"));
        Assert.False(MatchPredicates.Matches(new NewsFilter("moon"), new EarthquakePayload("4", "Mw", true)));
    }

    [Fact]
    public void Rule_ValidMatch_CapturesImmutableVersionedSnapshot()
    {
        var channels = new[] { NotificationChannel.Email, NotificationChannel.Slack };
        var rule = new AlertRule(Guid.NewGuid(), Guid.NewGuid(), " Name ", new NewsFilter("moon"), channels, version: 7);
        channels[0] = (NotificationChannel)99;
        var snapshot = Assert.IsType<MatchSnapshot>(MatchPredicates.Evaluate(rule, new NewsPayload("Moon")));
        Assert.Equal((rule.Id, rule.OwnerId, 7L, "Name", 3, 1),
            (snapshot.RuleId, snapshot.OwnerId, snapshot.RuleVersion, snapshot.RuleName, snapshot.ChannelMask, snapshot.PredicateVersion));
        Assert.Null(MatchPredicates.Evaluate(new AlertRule(rule.Id, rule.OwnerId, rule.Name, rule.Filter,
            [NotificationChannel.Email], enabled: false), new NewsPayload("Moon")));
    }

    [Fact]
    public void Rule_InvalidIdentityNameVersionOrChannels_Rejects()
    {
        var id = Guid.NewGuid();
        var filter = new NewsFilter("moon");
        Assert.Throws<DomainValidationException>(() => new AlertRule(Guid.Empty, id, "name", filter, [NotificationChannel.Email]));
        Assert.Throws<DomainValidationException>(() => new AlertRule(id, Guid.Empty, "name", filter, [NotificationChannel.Email]));
        Assert.Throws<DomainValidationException>(() => new AlertRule(id, id, " ", filter, [NotificationChannel.Email]));
        Assert.Throws<DomainValidationException>(() => new AlertRule(id, id, new string('n', 101), filter, [NotificationChannel.Email]));
        Assert.Throws<DomainValidationException>(() => new AlertRule(id, id, "name", filter, [], version: 1));
        Assert.Throws<DomainValidationException>(() => new AlertRule(id, id, "name", filter, [NotificationChannel.Email, NotificationChannel.Email]));
        Assert.Throws<DomainValidationException>(() => new AlertRule(id, id, "name", filter, [(NotificationChannel)99]));
        Assert.Throws<DomainValidationException>(() => new AlertRule(id, id, "name", filter, [NotificationChannel.Email], version: 0));
        Assert.Throws<DomainValidationException>(() => new AlertRule(id, id, "name", null!, [NotificationChannel.Email]));
        Assert.Throws<DomainValidationException>(() => new AlertRule(id, id, "name", filter, null!));
    }
}
