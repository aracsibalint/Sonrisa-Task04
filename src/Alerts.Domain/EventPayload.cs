using System.Text;

namespace Alerts.Domain;

public enum EventCategory { News, Market, Earthquake }

public abstract record EventPayload
{
    private protected EventPayload() { }
    public abstract EventCategory Category { get; }
}

public sealed record NewsPayload : EventPayload
{
    public override EventCategory Category => EventCategory.News;
    public string Title { get; }
    public string? Summary { get; }
    public string? ArticleUrl { get; }
    public bool SummaryTruncated { get; }
    public string MatchingText => Summary is null ? Title : $"{Title} {Summary}";

    public NewsPayload(string title, string? summary = null, string? articleUrl = null, bool summaryTruncated = false)
    {
        Title = PlainText.Normalize(title, 500);
        Summary = PlainText.Optional(summary, 4000, allowWhitespaceControls: true);
        Validate.Require(Encoding.UTF8.GetByteCount(MatchingText) <= 16 * 1024, "NewsTextTooLarge");
        ArticleUrl = articleUrl is null ? null : ArticleLink.Validate(articleUrl);
        SummaryTruncated = summaryTruncated;
    }
}

public sealed record MarketPayload : EventPayload
{
    public const string SupportedInstrument = "BTCUSDT";
    public const string SupportedQuoteCurrency = "USDT";
    public const string SupportedComparison = "binance-rolling-24h";
    public override EventCategory Category => EventCategory.Market;
    public CanonicalDecimal OpenPrice { get; }
    public CanonicalDecimal LastPrice { get; }
    public MillisecondUtc WindowStart { get; }
    public MillisecondUtc WindowEnd { get; }
    /// <summary>Display-only decimal result; division may round. Never use this for matching.</summary>
    public decimal SignedMovementPercent => checked(100m * (LastPrice.Value - OpenPrice.Value) / OpenPrice.Value);

    public MarketPayload(string instrument, string quoteCurrency, string comparisonKind,
        string openPrice, string lastPrice, MillisecondUtc windowStart, MillisecondUtc windowEnd)
    {
        Validate.Require(instrument == SupportedInstrument && quoteCurrency == SupportedQuoteCurrency
            && comparisonKind == SupportedComparison, "UnsupportedMarket");
        ArgumentNullException.ThrowIfNull(windowStart);
        ArgumentNullException.ThrowIfNull(windowEnd);
        Validate.Require(windowEnd.UnixMilliseconds - windowStart.UnixMilliseconds == 86_400_000, "InvalidMarketWindow");
        OpenPrice = CanonicalDecimal.Parse(openPrice, DecimalKind.Price);
        LastPrice = CanonicalDecimal.Parse(lastPrice, DecimalKind.Price);
        WindowStart = windowStart;
        WindowEnd = windowEnd;
    }
}

public sealed record EarthquakePayload : EventPayload
{
    public override EventCategory Category => EventCategory.Earthquake;
    public CanonicalDecimal Magnitude { get; }
    public string? Place { get; }

    public EarthquakePayload(string magnitude, string magnitudeScale, bool simulated, string? place = null)
    {
        Validate.Require(magnitudeScale == "Mw" && simulated, "UnsupportedEarthquake");
        Magnitude = CanonicalDecimal.Parse(magnitude, DecimalKind.Magnitude);
        Place = PlainText.Optional(place, 200, allowWhitespaceControls: true);
    }
}

public static class ArticleLink
{
    public static string Validate(string value)
    {
        PlainText.ValidateUnicode(value);
        Alerts.Domain.Validate.Require(value is { Length: > 0 and <= 2048 }
            && value.All(c => !char.IsWhiteSpace(c) && c != '\\'), "InvalidArticleUrl");
        Alerts.Domain.Validate.Require(Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == "https" || uri.Scheme == "http") && uri.UserInfo.Length == 0
            && uri.Host.Length > 0, "InvalidArticleUrl");
        return value;
    }
}
