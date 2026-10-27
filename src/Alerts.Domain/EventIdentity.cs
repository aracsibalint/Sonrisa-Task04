using System.Buffers;
using System.Globalization;
using System.Text;

namespace Alerts.Domain;

public enum EventOrigin { Manual, Webhook, Rss, Market }

public sealed record EventIdentity
{
    public Guid SourceId { get; }
    public Guid? ActorId { get; }
    public string ExternalKey { get; }
    public EventOrigin Origin { get; }
    public MillisecondUtc? ScheduledSlot { get; }

    private EventIdentity(Guid sourceId, EventOrigin origin, Guid? actorId, MillisecondUtc? slot, params string[] parts)
    {
        SourceId = Validate.Id(sourceId);
        Origin = origin;
        ActorId = actorId;
        ScheduledSlot = slot;
        ExternalKey = "v1|" + string.Join('|', parts.Select(part => Convert.ToBase64String(Encoding.UTF8.GetBytes(part))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_')));
        Validate.Require(ExternalKey.Length <= 12288, "ExternalKeyTooLong");
    }

    public static EventIdentity Manual(Guid sourceId, Guid authenticatedActorId, string externalId)
        => new(sourceId, EventOrigin.Manual, Validate.Id(authenticatedActorId), null,
            authenticatedActorId.ToString("D"), ExactExternalId(externalId, 512, 2048));

    public static EventIdentity Webhook(Guid credentialMappedSourceId, string externalId)
        => new(credentialMappedSourceId, EventOrigin.Webhook, null, null, ExactExternalId(externalId, 512, 2048));

    public static EventIdentity Rss(Guid sourceId, int identityVersion, string? decodedGuid, string? articleLink)
    {
        Validate.Require(identityVersion > 0, "InvalidVersion");
        var useGuid = !string.IsNullOrWhiteSpace(decodedGuid);
        var value = useGuid ? ExactExternalId(decodedGuid!, 2048, 8192)
            : NormalizeRssLink(articleLink ?? throw new DomainValidationException("MissingRssIdentity"));
        return new(sourceId, EventOrigin.Rss, null, null, identityVersion.ToString(CultureInfo.InvariantCulture),
            useGuid ? "guid" : "link", value);
    }

    /// <summary>The caller captures this slot before fetching, never from response/receipt time.</summary>
    public static EventIdentity Market(Guid sourceId, int scheduleVersion, MillisecondUtc scheduledSlot)
    {
        ArgumentNullException.ThrowIfNull(scheduledSlot);
        Validate.Require(scheduleVersion > 0, "InvalidVersion");
        Validate.Require(scheduledSlot.UnixMilliseconds % 60_000 == 0, "InvalidMarketSlot");
        return new(sourceId, EventOrigin.Market, null, scheduledSlot, MarketPayload.SupportedInstrument,
            MarketPayload.SupportedComparison, scheduleVersion.ToString(CultureInfo.InvariantCulture),
            scheduledSlot.UnixMilliseconds.ToString(CultureInfo.InvariantCulture));
    }

    private static string ExactExternalId(string value, int maximumScalars, int maximumBytes)
    {
        Validate.Require(!string.IsNullOrWhiteSpace(value), "InvalidExternalId");
        var remaining = value.AsSpan();
        var count = 0;
        while (!remaining.IsEmpty)
        {
            Validate.Require(Rune.DecodeFromUtf16(remaining, out var rune, out var consumed) == OperationStatus.Done,
                "InvalidUnicode");
            Validate.Require(!Rune.IsControl(rune) && ++count <= maximumScalars, "InvalidExternalId");
            remaining = remaining[consumed..];
        }
        Validate.Require(Encoding.UTF8.GetByteCount(value) <= maximumBytes, "InvalidExternalId");
        return value; // No trim, NFC or case folding of opaque IDs.
    }

    private static string NormalizeRssLink(string value)
    {
        ExactExternalId(value, 2048, 8192);
        Validate.Require(Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" && uri.UserInfo.Length == 0
            && !value.Any(char.IsWhiteSpace) && !value.Contains('\\'), "InvalidArticleUrl");
        // Preserve the supplied path/query bytes; Uri.AbsoluteUri would also canonicalize dot segments.
        var start = value.IndexOf("://", StringComparison.Ordinal);
        Validate.Require(start > 0, "InvalidArticleUrl");
        var suffixStart = value.IndexOfAny(['/', '?', '#'], start + 3);
        var suffix = suffixStart < 0 ? "" : value[suffixStart..];
        var fragment = suffix.IndexOf('#');
        if (fragment >= 0) suffix = suffix[..fragment];
        if (!suffix.StartsWith('/')) suffix = "/" + suffix;
        var host = uri.IdnHost.ToLowerInvariant();
        if (uri.HostNameType == UriHostNameType.IPv6) host = "[" + host.Trim('[', ']') + "]";
        var authority = uri.Scheme.ToLowerInvariant() + "://" + host
            + (uri.IsDefaultPort ? "" : ":" + uri.Port.ToString(CultureInfo.InvariantCulture));
        return authority + suffix;
    }
}
