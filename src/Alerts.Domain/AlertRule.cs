using System.Numerics;

namespace Alerts.Domain;

public enum NotificationChannel { Email = 1, Slack = 2 }

public abstract record RuleFilter
{
    private protected RuleFilter() { }
    public abstract EventCategory Category { get; }
}

public sealed record NewsFilter : RuleFilter
{
    public override EventCategory Category => EventCategory.News;
    public string Keyword { get; }
    public NewsFilter(string keyword) => Keyword = PlainText.Normalize(keyword, 100, allowWhitespaceControls: true);
}

public sealed record MarketFilter : RuleFilter
{
    public override EventCategory Category => EventCategory.Market;
    public CanonicalDecimal ThresholdPercent { get; }
    public MarketFilter(string thresholdPercent, string instrument = MarketPayload.SupportedInstrument,
        string comparisonKind = MarketPayload.SupportedComparison)
    {
        Validate.Require(instrument == MarketPayload.SupportedInstrument
            && comparisonKind == MarketPayload.SupportedComparison, "UnsupportedMarket");
        ThresholdPercent = CanonicalDecimal.Parse(thresholdPercent, DecimalKind.PercentThreshold);
    }
}

public sealed record EarthquakeFilter : RuleFilter
{
    public override EventCategory Category => EventCategory.Earthquake;
    public CanonicalDecimal MinimumMagnitude { get; }
    public EarthquakeFilter(string minimumMagnitude, string magnitudeScale = "Mw")
    {
        Validate.Require(magnitudeScale == "Mw", "UnsupportedEarthquake");
        MinimumMagnitude = CanonicalDecimal.Parse(minimumMagnitude, DecimalKind.Magnitude);
    }
}

public sealed record AlertRule
{
    public Guid Id { get; }
    public Guid OwnerId { get; }
    public string Name { get; }
    public RuleFilter Filter { get; }
    public EventCategory Category => Filter.Category;
    public int ChannelMask { get; }
    public bool Enabled { get; }
    public long Version { get; }

    public AlertRule(Guid id, Guid ownerId, string name, RuleFilter filter,
        IEnumerable<NotificationChannel> channels, bool enabled = true, long version = 1)
    {
        Id = Validate.Id(id);
        OwnerId = Validate.Id(ownerId);
        Name = PlainText.Normalize(name, 100);
        Validate.Require(filter is not null, "MissingFilter");
        Validate.Require(channels is not null, "InvalidChannels");
        var mask = 0;
        var count = 0;
        foreach (var channel in channels)
        {
            Validate.Require(Enum.IsDefined(channel) && (mask & (int)channel) == 0, "InvalidChannels");
            mask |= (int)channel;
            Validate.Require(++count <= 2, "InvalidChannels");
        }
        Validate.Require(count > 0, "InvalidChannels");
        Validate.Require(version > 0, "InvalidVersion");
        Filter = filter;
        ChannelMask = mask;
        Enabled = enabled;
        Version = version;
    }
}

/// <summary>Immutable value for later intent snapshots, not a persisted Match entity.</summary>
public sealed record MatchSnapshot(Guid RuleId, Guid OwnerId, long RuleVersion, string RuleName,
    RuleFilter Filter, int ChannelMask, int PredicateVersion);

public static class MatchPredicates
{
    public const int Version = 1;

    public static bool Matches(RuleFilter filter, EventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(payload);
        return (filter, payload) switch
        {
            (NewsFilter rule, NewsPayload news) => news.MatchingText.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase),
            (MarketFilter rule, MarketPayload market) => MatchesMarket(rule, market),
            (EarthquakeFilter rule, EarthquakePayload earthquake) => earthquake.Magnitude.Value >= rule.MinimumMagnitude.Value,
            _ => false
        };
    }

    private static bool MatchesMarket(MarketFilter rule, MarketPayload market)
    {
        // Validated prices/thresholds have at most eight fractional places. Decimal multiplication
        // can silently round even in checked code; lift coefficients BEFORE subtraction/products.
        var open = ScaleToEightPlaces(market.OpenPrice.Value);
        var last = ScaleToEightPlaces(market.LastPrice.Value);
        var threshold = ScaleToEightPlaces(rule.ThresholdPercent.Value);
        // Both sides now have denominator 10^16. No division or decimal arithmetic is involved.
        return 100 * BigInteger.Abs(last - open) * 100_000_000 >= threshold * open;
    }

    private static BigInteger ScaleToEightPlaces(decimal value)
    {
        var bits = decimal.GetBits(value);
        var coefficient = (BigInteger)(uint)bits[0]
            + ((BigInteger)(uint)bits[1] << 32)
            + ((BigInteger)(uint)bits[2] << 64);
        var scale = (bits[3] >> 16) & 0xff;
        var scaled = coefficient * BigInteger.Pow(10, 8 - scale);
        return bits[3] < 0 ? -scaled : scaled;
    }

    public static MatchSnapshot? Evaluate(AlertRule rule, EventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(payload);
        return rule.Enabled && Matches(rule.Filter, payload)
            ? new(rule.Id, rule.OwnerId, rule.Version, rule.Name, rule.Filter, rule.ChannelMask, Version)
            : null;
    }
}
