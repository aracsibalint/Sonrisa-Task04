using Alerts.Application;
using Alerts.Application.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alerts.Infrastructure.Persistence;

public sealed class RulePersistence(IServiceScopeFactory scopes) : IRulePersistence
{
    private sealed record StoredRule(string Id, string Name, string Category, string? Keyword, string? Instrument,
        string? ComparisonKind, string? ThresholdPercentText, string? MinimumMagnitudeText, string? MagnitudeScale,
        bool EmailSelected, bool SlackSelected, bool Enabled, long Version, long CreatedAtUtcMs, long UpdatedAtUtcMs);
    private sealed record DestinationState(string Channel, string Id, long ConfigurationVersion, bool IsAvailable, long? RevokedAtUtcMs);
    private sealed record ResolvedDestination(string Channel, bool Available, string? Reason, string? Id, long? ConfigurationVersion);

    public async Task<IReadOnlyList<ChannelAvailability>> ChannelsAsync(string ownerId, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlertsDbContext>();
        var resolved = await ResolveDestinationsAsync(db, ownerId, ct).ConfigureAwait(false);
        return resolved.Select(x => new ChannelAvailability(x.Channel, x.Available, x.Reason)).ToArray();
    }

    public async Task<RulePage> ListAsync(string ownerId, string? category, bool? enabled, int take, RuleSortKey? after, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlertsDbContext>();
        var query = db.Set<RuleRow>().AsNoTracking().Where(x => x.OwnerId == ownerId && x.DeletedAtUtcMs == null);
        if (category is not null) query = query.Where(x => x.Category == category);
        if (enabled is not null) query = query.Where(x => x.Enabled == enabled);
        if (after is not null) query = query.Where(x => x.CreatedAtUtcMs < after.CreatedAtUtcMs
            || x.CreatedAtUtcMs == after.CreatedAtUtcMs && string.Compare(x.Id, after.Id) < 0);
        var rows = await Project(query.OrderByDescending(x => x.CreatedAtUtcMs).ThenByDescending(x => x.Id).Take(take + 1))
            .ToListAsync(ct).ConfigureAwait(false);
        var more = rows.Count > take;
        if (more) rows.RemoveAt(take);
        return new(rows.Select(Map).ToArray(), more && rows.Count != 0 ? new(rows[^1].CreatedAtUtcMs, rows[^1].Id) : null);
    }

    public async Task<RuleView?> GetAsync(string ownerId, string id, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlertsDbContext>();
        var row = await Project(db.Set<RuleRow>().AsNoTracking().Where(
            x => x.OwnerId == ownerId && x.Id == id && x.DeletedAtUtcMs == null)).SingleOrDefaultAsync(ct).ConfigureAwait(false);
        return row is null ? null : Map(row);
    }

    public Task<RuleView> CreateAsync(MutationAdmission admission, string ownerId, RuleDraft draft, CancellationToken ct) =>
        Write(admission, async (db, token) =>
        {
            await RequireChannels(db, ownerId, draft.Channels, token).ConfigureAwait(false);
            if (draft.Enabled) await RequireQuota(db, ownerId, token).ConfigureAwait(false);
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var row = NewRow(ownerId, draft, now);
            db.Set<RuleRow>().Add(row);
            return Map(row);
        }, ct);

    public Task<RuleView> ReplaceAsync(MutationAdmission admission, string ownerId, string id, RuleDraft draft,
        long expectedVersion, CancellationToken ct) => Write(admission, async (db, token) =>
    {
        var row = await db.Set<RuleRow>().SingleOrDefaultAsync(
            x => x.OwnerId == ownerId && x.Id == id && x.DeletedAtUtcMs == null, token).ConfigureAwait(false)
            ?? throw new RuleOperationException(RuleFailure.NotFound);
        if (row.Version != expectedVersion) throw new RuleOperationException(RuleFailure.VersionConflict);
        if (row.Category != draft.Category) throw new RuleOperationException(RuleFailure.ImmutableCategory);
        await RequireChannels(db, ownerId, draft.Channels, token).ConfigureAwait(false);
        if (!row.Enabled && draft.Enabled) await RequireQuota(db, ownerId, token).ConfigureAwait(false);
        Apply(row, draft);
        row.Version = checked(row.Version + 1);
        row.UpdatedAtUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return Map(row);
    }, ct);

    public Task DeleteAsync(MutationAdmission admission, string ownerId, string id, long expectedVersion, CancellationToken ct) =>
        Write(admission, async (db, token) =>
        {
            var row = await db.Set<RuleRow>().SingleOrDefaultAsync(
                x => x.OwnerId == ownerId && x.Id == id && x.DeletedAtUtcMs == null, token).ConfigureAwait(false)
                ?? throw new RuleOperationException(RuleFailure.NotFound);
            if (row.Version != expectedVersion) throw new RuleOperationException(RuleFailure.VersionConflict);
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            row.Enabled = false;
            row.DeletedAtUtcMs = now;
            row.UpdatedAtUtcMs = now;
            row.Version = checked(row.Version + 1);
            return true;
        }, ct);

    private async Task<T> Write<T>(MutationAdmission admission, Func<AlertsDbContext, CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PersistenceUnitOfWork>()
            .ExecuteAsync(admission, MutationPurpose.Normal, action, ct).ConfigureAwait(false);
    }

    private static async Task RequireChannels(AlertsDbContext db, string ownerId, IReadOnlyList<string> channels, CancellationToken ct)
    {
        var resolved = await ResolveDestinationsAsync(db, ownerId, ct).ConfigureAwait(false);
        if (channels.Any(channel => !resolved.Any(x => x.Channel == channel && x.Available)))
            throw new RuleOperationException(RuleFailure.ChannelUnavailable);
    }

    private static async Task<IReadOnlyList<ResolvedDestination>> ResolveDestinationsAsync(
        AlertsDbContext db, string ownerId, CancellationToken ct)
    {
        var rows = await db.Set<ChannelDestinationRow>().AsNoTracking().Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.ConfigurationVersion)
            .Select(x => new DestinationState(x.Channel, x.Id, x.ConfigurationVersion, x.IsAvailable, x.RevokedAtUtcMs))
            .ToListAsync(ct).ConfigureAwait(false);
        return new[] { "Email", "Slack" }.Select(channel =>
        {
            // The partial unique index guarantees at most one canonical active row. Historical
            // configuration versions only determine the safe unavailable reason when none is active.
            var active = rows.SingleOrDefault(x => x.Channel == channel && x.IsAvailable && x.RevokedAtUtcMs is null);
            if (active is not null)
                return new ResolvedDestination(channel, true, null, active.Id, active.ConfigurationVersion);
            var newest = rows.FirstOrDefault(x => x.Channel == channel);
            return new ResolvedDestination(channel, false, newest is null ? "NotProvisioned"
                : newest.RevokedAtUtcMs is not null ? "Revoked" : "Disabled", null, null);
        }).ToArray();
    }

    private static async Task RequireQuota(AlertsDbContext db, string ownerId, CancellationToken ct)
    {
        var ownerCount = await db.Set<RuleRow>().CountAsync(x => x.OwnerId == ownerId && x.Enabled && x.DeletedAtUtcMs == null, ct).ConfigureAwait(false);
        var globalCount = await db.Set<RuleRow>().CountAsync(x => x.Enabled && x.DeletedAtUtcMs == null, ct).ConfigureAwait(false);
        if (ownerCount >= 20 || globalCount >= 100) throw new RuleOperationException(RuleFailure.RuleLimitReached);
    }

    private static RuleRow NewRow(string ownerId, RuleDraft draft, long now)
    {
        var row = new RuleRow { Id = Guid.NewGuid().ToString("D"), OwnerId = ownerId, Version = 1, CreatedAtUtcMs = now, UpdatedAtUtcMs = now };
        Apply(row, draft);
        return row;
    }
    private static void Apply(RuleRow row, RuleDraft draft)
    {
        row.Name = draft.Name; row.Category = draft.Category; row.Enabled = draft.Enabled;
        row.EmailSelected = draft.Channels.Contains("Email"); row.SlackSelected = draft.Channels.Contains("Slack");
        row.Keyword = draft.Filter.Keyword; row.Instrument = draft.Filter.Instrument;
        row.ComparisonKind = draft.Filter.ComparisonKind; row.ThresholdPercentText = draft.Filter.ThresholdPercent;
        row.MinimumMagnitudeText = draft.Filter.MinimumMagnitude; row.MagnitudeScale = draft.Filter.MagnitudeScale;
    }
    private static RuleView Map(RuleRow x) => new(x.Id, x.Name, x.Category,
        new(x.Category, x.Keyword, x.Instrument, x.ComparisonKind, x.ThresholdPercentText, x.MinimumMagnitudeText, x.MagnitudeScale),
        new[] { x.EmailSelected ? "Email" : null, x.SlackSelected ? "Slack" : null }.OfType<string>().ToArray(),
        x.Enabled, x.Version, x.CreatedAtUtcMs, x.UpdatedAtUtcMs);
    private static IQueryable<StoredRule> Project(IQueryable<RuleRow> query) => query.Select(x => new StoredRule(
        x.Id, x.Name, x.Category, x.Keyword, x.Instrument, x.ComparisonKind, x.ThresholdPercentText,
        x.MinimumMagnitudeText, x.MagnitudeScale, x.EmailSelected, x.SlackSelected, x.Enabled, x.Version,
        x.CreatedAtUtcMs, x.UpdatedAtUtcMs));
    private static RuleView Map(StoredRule x) => new(x.Id, x.Name, x.Category,
        new(x.Category, x.Keyword, x.Instrument, x.ComparisonKind, x.ThresholdPercentText, x.MinimumMagnitudeText, x.MagnitudeScale),
        new[] { x.EmailSelected ? "Email" : null, x.SlackSelected ? "Slack" : null }.OfType<string>().ToArray(),
        x.Enabled, x.Version, x.CreatedAtUtcMs, x.UpdatedAtUtcMs);
}
