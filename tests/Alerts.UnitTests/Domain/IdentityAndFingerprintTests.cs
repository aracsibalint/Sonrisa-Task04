using System.Globalization;
using System.Text;
using Alerts.Domain;
using Xunit;

namespace Alerts.UnitTests.Domain;

public sealed class IdentityAndFingerprintTests
{
    private static readonly Guid Source = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Actor = Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public void Identity_SourceActorAndExactExternalId_AreScopedWithoutHiddenNormalization()
    {
        var original = EventIdentity.Manual(Source, Actor, "Exact|ID");
        Assert.Equal(original, EventIdentity.Manual(Source, Actor, "Exact|ID"));
        Assert.NotEqual(original, EventIdentity.Manual(Source, Guid.NewGuid(), "Exact|ID"));
        Assert.NotEqual(original, EventIdentity.Manual(Guid.NewGuid(), Actor, "Exact|ID"));
        Assert.NotEqual(original.ExternalKey, EventIdentity.Manual(Source, Actor, "exact|ID").ExternalKey);
        Assert.NotEqual(original.ExternalKey, EventIdentity.Manual(Source, Actor, " Exact|ID ").ExternalKey);
        Assert.NotEqual(EventIdentity.Webhook(Source, "é"), EventIdentity.Webhook(Source, "e\u0301"));
        Assert.NotEqual(EventIdentity.Webhook(Source, "a|b"), EventIdentity.Webhook(Source, "ab"));
        Assert.Equal("v1|eA", EventIdentity.Webhook(Source, "x").ExternalKey);
        Assert.Throws<DomainValidationException>(() => EventIdentity.Webhook(Source, "\ud800"));
        Assert.Throws<DomainValidationException>(() => EventIdentity.Webhook(Source, "bad\nID"));
        Assert.Throws<DomainValidationException>(() => EventIdentity.Webhook(Source, new string('a', 513)));
        Assert.Throws<DomainValidationException>(() => EventIdentity.Manual(Source, Guid.Empty, "x"));
    }

    [Fact]
    public void RssIdentity_GuidTakesPrecedence_LinkNormalizationPreservesPathAndQuery()
    {
        Assert.Equal(EventIdentity.Rss(Source, 1, "opaque-GUID", "https://one.test"), EventIdentity.Rss(Source, 1, "opaque-GUID", "https://two.test"));
        Assert.Equal(EventIdentity.Rss(Source, 1, null, "HTTPS://EXAMPLE.TEST:443/a/../B?q=One#fragment"),
            EventIdentity.Rss(Source, 1, null, "https://example.test/a/../B?q=One"));
        Assert.NotEqual(EventIdentity.Rss(Source, 1, null, "https://example.test/a/../B?q=One"),
            EventIdentity.Rss(Source, 1, null, "https://example.test/B?q=One"));
        Assert.Throws<DomainValidationException>(() => EventIdentity.Rss(Source, 1, null, null));
        Assert.Throws<DomainValidationException>(() => EventIdentity.Rss(Source, 1, new string('a', 2049), null));
    }

    [Fact]
    public void MarketIdentity_UsesCapturedSlotAndSchedule_NotPricesOrReceipt()
    {
        var current = EventIdentity.Market(Source, 1, MatchingTests.End);
        Assert.Equal(current, EventIdentity.Market(Source, 1, MatchingTests.End));
        Assert.NotEqual(current, EventIdentity.Market(Source, 2, MatchingTests.End));
        Assert.NotEqual(current, EventIdentity.Market(Source, 1, MillisecondUtc.From(MatchingTests.End.Value.AddMinutes(1))));
        Assert.Throws<DomainValidationException>(() => EventIdentity.Market(Source, 1, MillisecondUtc.From(MatchingTests.End.Value.AddMilliseconds(1))));
        var candidate = new EventCandidate(current, MatchingTests.End, MatchingTests.Market("105"));
        candidate.ValidateNewAt(MillisecondUtc.From(MatchingTests.End.Value.AddMilliseconds(59999)));
        Assert.Throws<DomainValidationException>(() => candidate.ValidateNewAt(MillisecondUtc.From(MatchingTests.End.Value.AddMinutes(1))));
        Assert.Throws<DomainValidationException>(() => candidate.ValidateNewAt(MillisecondUtc.From(MatchingTests.End.Value.AddMilliseconds(-1))));
    }

    [Theory]
    [InlineData(-60000, true)]
    [InlineData(-60001, false)]
    [InlineData(300000, true)]
    [InlineData(300001, false)]
    public void ValidateNewAt_ManualMarket_EnforcesInclusiveFreshness(long receiptOffsetMilliseconds, bool valid)
    {
        var candidate = new EventCandidate(EventIdentity.Manual(Source, Actor, "x"), MatchingTests.End, MatchingTests.Market("105"));
        var received = MillisecondUtc.From(MatchingTests.End.Value.AddMilliseconds(receiptOffsetMilliseconds));
        if (valid) candidate.ValidateNewAt(received);
        else Assert.Throws<DomainValidationException>(() => candidate.ValidateNewAt(received));
    }

    [Fact]
    public void Candidate_CategoryAndRelativeFutureChecks_AreSeparateFromContentEquality()
    {
        var identity = EventIdentity.Webhook(Source, "x");
        var candidate = new EventCandidate(identity, MatchingTests.End, new EarthquakePayload("4.5", "Mw", true));
        candidate.ValidateNewAt(MillisecondUtc.From(MatchingTests.End.Value.AddMinutes(-5)));
        candidate.ValidateNewAt(MillisecondUtc.From(MatchingTests.End.Value.AddYears(10))); // Past is permitted.
        Assert.Throws<DomainValidationException>(() => candidate.ValidateNewAt(MillisecondUtc.From(MatchingTests.End.Value.AddMilliseconds(-300001))));
        Assert.Throws<DomainValidationException>(() => new EventCandidate(identity, MatchingTests.End, new NewsPayload("title")));
        Assert.Throws<DomainValidationException>(() => new EventCandidate(EventIdentity.Manual(Source, Actor, "x"), MatchingTests.Start, MatchingTests.Market("105")));
        Assert.Throws<DomainValidationException>(() => new EventCandidate(EventIdentity.Manual(Source, Actor, "x"), MatchingTests.End, new NewsPayload("title", summaryTruncated: true)));
        Assert.True(candidate.HasSameContent(new EventCandidate(identity, MatchingTests.End, new EarthquakePayload("4.50", "Mw", true))));
        Assert.False(candidate.HasSameContent(new EventCandidate(identity, MatchingTests.End, new EarthquakePayload("4.51", "Mw", true))));
    }

    [Theory]
    [InlineData(EventCategory.News,
        "{\"schemaVersion\":1,\"category\":\"news\",\"occurredAt\":\"2026-01-01T00:00:00.000Z\",\"data\":{\"title\":\"Moon\",\"summary\":null,\"articleUrl\":null,\"summaryTruncated\":false}}",
        "6f6cdedfcde31adf98d9e69332cce8e6c5d9a914f2c21316aa385b19a827bb89")]
    [InlineData(EventCategory.Market,
        "{\"schemaVersion\":1,\"category\":\"market\",\"occurredAt\":\"2026-01-01T00:00:00.000Z\",\"data\":{\"instrument\":\"BTCUSDT\",\"quoteCurrency\":\"USDT\",\"comparisonKind\":\"binance-rolling-24h\",\"windowStart\":\"2025-12-31T00:00:00.000Z\",\"windowEnd\":\"2026-01-01T00:00:00.000Z\",\"openPrice\":\"100\",\"lastPrice\":\"105\"}}",
        "57f014dfea61457e1d48869e103933f89027c8270f8b438c33d7702faf557bc7")]
    [InlineData(EventCategory.Earthquake,
        "{\"schemaVersion\":1,\"category\":\"earthquake\",\"occurredAt\":\"2026-01-01T00:00:00.000Z\",\"data\":{\"magnitude\":\"4.5\",\"magnitudeScale\":\"Mw\",\"place\":null,\"simulated\":true}}",
        "5be21c0ce2657d8bca5bb92f10d834ed8da62125691ec6a768b80e656e4fe6c3")]
    public void CanonicalBytes_V1_MatchesHandAuthoredGoldenBytesAndSha256(EventCategory category, string expected, string hash)
    {
        EventPayload payload = category switch
        {
            EventCategory.News => new NewsPayload(" Moon "),
            EventCategory.Market => MatchingTests.Market("105.00000000"),
            _ => new EarthquakePayload("4.50", "Mw", true)
        };
        var candidate = new EventCandidate(EventIdentity.Manual(Source, Actor, "x"), MatchingTests.End, payload);
        Assert.Equal(expected, Encoding.UTF8.GetString(candidate.CanonicalBytes()));
        Assert.Equal(hash, candidate.FingerprintHex());
    }

    [Fact]
    public void Fingerprint_NormalizedContentAndDifferentActorOrCulture_ProducesSameBytes()
    {
        var first = new EventCandidate(EventIdentity.Manual(Source, Actor, "one"), MatchingTests.End, new NewsPayload(" Cafe\u0301  launch ", " moon\nnews "));
        var second = new EventCandidate(EventIdentity.Manual(Source, Guid.NewGuid(), "two"), MatchingTests.End, new NewsPayload("Café launch", "moon news"));
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.Equal(first.CanonicalBytes(), second.CanonicalBytes());
            Assert.True(first.HasSameContent(second));
        }
        finally { CultureInfo.CurrentCulture = previous; }
        var bytes = first.CanonicalBytes();
        bytes[0] = 0;
        Assert.NotEqual(bytes, first.CanonicalBytes());
    }
}
