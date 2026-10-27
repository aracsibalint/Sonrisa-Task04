using Alerts.Domain;
using System.Diagnostics;

namespace Alerts.Application.Rules;

public sealed record RuleFilterData(string Category, string? Keyword = null, string? Instrument = null,
    string? ComparisonKind = null, string? ThresholdPercent = null, string? MinimumMagnitude = null,
    string? MagnitudeScale = null);

public sealed record RuleDraft(string Name, string Category, RuleFilterData Filter,
    IReadOnlyList<string> Channels, bool Enabled);

public sealed record RuleView(string Id, string Name, string Category, RuleFilterData Filter,
    IReadOnlyList<string> Channels, bool Enabled, long Version, long CreatedAtUtcMs, long UpdatedAtUtcMs);

public sealed record RulePage(IReadOnlyList<RuleView> Items, RuleSortKey? Next);
public sealed record RuleSortKey(long CreatedAtUtcMs, string Id);
public sealed record ChannelAvailability(string Channel, bool Available, string? Reason);

public enum RuleFailure { NotFound, VersionConflict, RuleLimitReached, ChannelUnavailable, ImmutableCategory }
public sealed class RuleOperationException(RuleFailure failure) : Exception(failure.ToString())
{
    public RuleFailure Failure { get; } = failure;
}

public interface IRulePersistence
{
    Task<IReadOnlyList<ChannelAvailability>> ChannelsAsync(string ownerId, CancellationToken cancellationToken);
    Task<RulePage> ListAsync(string ownerId, string? category, bool? enabled, int take,
        RuleSortKey? after, CancellationToken cancellationToken);
    Task<RuleView?> GetAsync(string ownerId, string id, CancellationToken cancellationToken);
    Task<RuleView> CreateAsync(MutationAdmission admission, string ownerId, RuleDraft draft,
        CancellationToken cancellationToken);
    Task<RuleView> ReplaceAsync(MutationAdmission admission, string ownerId, string id, RuleDraft draft,
        long expectedVersion, CancellationToken cancellationToken);
    Task DeleteAsync(MutationAdmission admission, string ownerId, string id, long expectedVersion,
        CancellationToken cancellationToken);
}

public sealed class RuleUseCases(IMutationGate gate, IRulePersistence persistence)
{
    public Task<IReadOnlyList<ChannelAvailability>> ChannelsAsync(string ownerId, CancellationToken ct) =>
        persistence.ChannelsAsync(Owner(ownerId), ct);

    public Task<RulePage> ListAsync(string ownerId, string? category, bool? enabled, int take,
        RuleSortKey? after, CancellationToken ct) =>
        persistence.ListAsync(Owner(ownerId), category is null ? null : Category(category), enabled, take, after, ct);

    public Task<RuleView?> GetAsync(string ownerId, string id, CancellationToken ct) =>
        persistence.GetAsync(Owner(ownerId), Id(id), ct);

    public Task<RuleView> CreateAsync(string ownerId, RuleDraft draft, CancellationToken ct)
    {
        var validated = Validate(draft);
        return gate.ExecuteAsync((admission, token) => persistence.CreateAsync(admission, Owner(ownerId), validated, token), ct);
    }

    public Task<RuleView> ReplaceAsync(string ownerId, string id, RuleDraft draft, long version, CancellationToken ct)
    {
        if (version <= 0) throw new DomainValidationException("InvalidVersion");
        var validated = Validate(draft);
        return gate.ExecuteAsync((admission, token) => persistence.ReplaceAsync(admission, Owner(ownerId), Id(id), validated, version, token), ct);
    }

    public Task DeleteAsync(string ownerId, string id, long version, CancellationToken ct) =>
        gate.ExecuteAsync(async (admission, token) =>
        {
            await persistence.DeleteAsync(admission, Owner(ownerId), Id(id), version, token).ConfigureAwait(false);
            return true;
        }, ct);

    private static RuleDraft Validate(RuleDraft draft)
    {
        var category = Category(draft.Category);
        RuleFilter filter = category switch
        {
            "News" => new NewsFilter(draft.Filter.Keyword!),
            "Market" => new MarketFilter(draft.Filter.ThresholdPercent!, draft.Filter.Instrument!, draft.Filter.ComparisonKind!),
            "Earthquake" => new EarthquakeFilter(draft.Filter.MinimumMagnitude!, draft.Filter.MagnitudeScale!),
            _ => throw new DomainValidationException("InvalidCategory")
        };
        var channels = draft.Channels.Select(x => x switch
        {
            "Email" => NotificationChannel.Email,
            "Slack" => NotificationChannel.Slack,
            _ => throw new DomainValidationException("InvalidChannels")
        }).ToArray();
        var rule = new AlertRule(Guid.NewGuid(), Guid.NewGuid(), draft.Name, filter, channels, draft.Enabled);
        var data = filter switch
        {
            NewsFilter f => new RuleFilterData(category, Keyword: f.Keyword),
            MarketFilter f => new RuleFilterData(category, Instrument: MarketPayload.SupportedInstrument,
                ComparisonKind: MarketPayload.SupportedComparison, ThresholdPercent: f.ThresholdPercent.Text),
            EarthquakeFilter f => new RuleFilterData(category, MinimumMagnitude: f.MinimumMagnitude.Text, MagnitudeScale: "Mw"),
            _ => throw new UnreachableException()
        };
        return new(rule.Name, category, data,
            channels.Select(x => x == NotificationChannel.Email ? "Email" : "Slack").ToArray(), rule.Enabled);
    }

    private static string Category(string value) => value switch
    {
        "News" or "news" => "News", "Market" or "market" => "Market",
        "Earthquake" or "earthquake" => "Earthquake", _ => throw new DomainValidationException("InvalidCategory")
    };
    private static string Owner(string value) => Id(value);
    private static string Id(string value) => Guid.TryParseExact(value, "D", out var id) && id != Guid.Empty
        && id.ToString("D") == value ? value : throw new DomainValidationException("InvalidIdentifier");
}
