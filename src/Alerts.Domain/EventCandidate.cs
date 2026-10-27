using System.Security.Cryptography;
using System.Text.Json;

namespace Alerts.Domain;

public sealed record EventCandidate
{
    public const int FingerprintVersion = 1;
    public EventIdentity Identity { get; }
    public MillisecondUtc OccurredAt { get; }
    public EventPayload Payload { get; }

    public EventCandidate(EventIdentity identity, MillisecondUtc occurredAt, EventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(occurredAt);
        ArgumentNullException.ThrowIfNull(payload);
        Validate.Require(identity.Origin switch
        {
            EventOrigin.Manual => true,
            EventOrigin.Webhook => payload is EarthquakePayload,
            EventOrigin.Rss => payload is NewsPayload,
            EventOrigin.Market => payload is MarketPayload,
            _ => false
        }, "SourceCategoryMismatch");
        Validate.Require(payload is not MarketPayload market || occurredAt == market.WindowEnd, "MarketOccurrenceMismatch");
        Validate.Require(identity.Origin == EventOrigin.Rss || payload is not NewsPayload { SummaryTruncated: true },
            "InvalidTruncationProvenance");
        Identity = identity;
        OccurredAt = occurredAt;
        Payload = payload;
    }

    /// <summary>Call only for a new identity, after committed duplicates have been resolved.</summary>
    public void ValidateNewAt(MillisecondUtc receivedAt)
    {
        ArgumentNullException.ThrowIfNull(receivedAt);
        var age = receivedAt.UnixMilliseconds - OccurredAt.UnixMilliseconds;
        if (Payload is MarketPayload)
        {
            Validate.Require(age >= -60_000 && age <= 300_000, "StaleMarketObservation");
            if (Identity.ScheduledSlot is { } slot)
            {
                var elapsed = receivedAt.UnixMilliseconds - slot.UnixMilliseconds;
                Validate.Require(elapsed >= 0 && elapsed < 60_000, "ExpiredMarketSlot");
            }
        }
        else
        {
            Validate.Require(age >= -300_000, "FutureOccurrence");
        }
    }

    /// <summary>Canonical v1 bytes: fixed order, default System.Text.Json escaping, explicit null optionals.</summary>
    public byte[] CanonicalBytes()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", FingerprintVersion);
            writer.WriteString("category", Payload.Category.ToString().ToLowerInvariant());
            writer.WriteString("occurredAt", OccurredAt.Text);
            writer.WriteStartObject("data");
            switch (Payload)
            {
                case NewsPayload news:
                    writer.WriteString("title", news.Title);
                    writer.WriteString("summary", news.Summary);
                    writer.WriteString("articleUrl", news.ArticleUrl);
                    writer.WriteBoolean("summaryTruncated", news.SummaryTruncated);
                    break;
                case MarketPayload market:
                    writer.WriteString("instrument", MarketPayload.SupportedInstrument);
                    writer.WriteString("quoteCurrency", MarketPayload.SupportedQuoteCurrency);
                    writer.WriteString("comparisonKind", MarketPayload.SupportedComparison);
                    writer.WriteString("windowStart", market.WindowStart.Text);
                    writer.WriteString("windowEnd", market.WindowEnd.Text);
                    writer.WriteString("openPrice", market.OpenPrice.Text);
                    writer.WriteString("lastPrice", market.LastPrice.Text);
                    break;
                case EarthquakePayload earthquake:
                    writer.WriteString("magnitude", earthquake.Magnitude.Text);
                    writer.WriteString("magnitudeScale", "Mw");
                    writer.WriteString("place", earthquake.Place);
                    writer.WriteBoolean("simulated", true);
                    break;
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public string FingerprintHex() => Convert.ToHexStringLower(SHA256.HashData(CanonicalBytes()));

    public bool HasSameContent(EventCandidate other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return OccurredAt == other.OccurredAt && Payload == other.Payload;
    }
}
