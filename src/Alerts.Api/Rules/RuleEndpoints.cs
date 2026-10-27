using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Alerts.Application;
using Alerts.Application.Rules;
using Alerts.Domain;
using Alerts.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Alerts.Api.Rules;

public sealed record CreateRuleRequest(string? Name, string? Category, JsonElement Filter, string[]? Channels, bool? Enabled);
public sealed record UpdateRuleRequest(string? Name, string? Category, JsonElement Filter, string[]? Channels, bool? Enabled, long? Version);
public sealed record FilterResponse(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Keyword = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Instrument = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ComparisonKind = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ThresholdPercent = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MinimumMagnitude = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MagnitudeScale = null);
public sealed record RuleResponse(string Id, string Name, string Category, FilterResponse Filter,
    string[] Channels, bool Enabled, long Version, string CreatedAt, string UpdatedAt);
internal sealed record RuleCursor(string OwnerId, string? Category, bool? Enabled, int PageSize, long CreatedAtUtcMs, string Id);

public static class RuleEndpoints
{
    private static readonly HashSet<string> QueryFields = ["category", "enabled", "pageSize", "cursor"];

    public static void MapAlertsRules(this WebApplication app)
    {
        app.MapGet("/api/v1/rules", List).RequireAuthorization("Owner");
        app.MapPost("/api/v1/rules", Create).RequireAuthorization("Owner");
        app.MapGet("/api/v1/rules/{id}", Get).RequireAuthorization("Owner");
        app.MapPut("/api/v1/rules/{id}", Replace).RequireAuthorization("Owner");
        app.MapDelete("/api/v1/rules/{id}", Delete).RequireAuthorization("Owner");
    }

    private static async Task<IResult> List(HttpContext http, ClaimsPrincipal principal, RuleUseCases rules,
        IDataProtectionProvider protection, CancellationToken ct)
    {
        try
        {
            if (http.Request.Query.Keys.Any(x => !QueryFields.Contains(x))) return Problem(http, 400, "ValidationFailed");
            var category = Single(http, "category");
            var enabledText = Single(http, "enabled");
            bool? enabled = enabledText switch { null => null, "true" => true, "false" => false, _ => throw new DomainValidationException("InvalidEnabled") };
            var sizeText = Single(http, "pageSize");
            var size = sizeText is null ? 25 : int.TryParse(sizeText, out var parsed) && parsed is >= 1 and <= 100
                ? parsed : throw new DomainValidationException("InvalidPageSize");
            RuleSortKey? after = null;
            var cursorText = Single(http, "cursor", "InvalidCursor");
            var owner = Owner(principal);
            if (cursorText is not null)
            {
                if (cursorText.Length > 4096 || cursorText.Any(x => x > 127)) throw new DomainValidationException("InvalidCursor");
                RuleCursor cursor;
                try
                {
                    var json = protection.CreateProtector("alerts.rules.cursor.v1").Unprotect(cursorText);
                    cursor = JsonSerializer.Deserialize<RuleCursor>(json) ?? throw new Exception();
                }
                catch { return Problem(http, 400, "InvalidCursor"); }
                if (cursor.OwnerId != owner || cursor.Category != category || cursor.Enabled != enabled || cursor.PageSize != size)
                    return Problem(http, 400, "InvalidCursor");
                after = new(cursor.CreatedAtUtcMs, cursor.Id);
            }
            var page = await rules.ListAsync(owner, category, enabled, size, after, ct).ConfigureAwait(false);
            string? next = null;
            if (page.Next is not null)
                next = protection.CreateProtector("alerts.rules.cursor.v1").Protect(JsonSerializer.Serialize(
                    new RuleCursor(owner, category, enabled, size, page.Next.CreatedAtUtcMs, page.Next.Id)));
            return Results.Ok(new { items = page.Items.Select(Map), nextCursor = next });
        }
        catch (DomainValidationException e) { return Problem(http, 400, e.Code == "InvalidCursor" ? "InvalidCursor" : "ValidationFailed"); }
    }

    private static async Task<IResult> Create(HttpContext http, ClaimsPrincipal principal, RuleUseCases rules,
        IOptions<HttpJsonOptions> json, CancellationToken ct)
    {
        try
        {
            var request = await ApiJsonBody.ReadAsync<CreateRuleRequest>(http.Request, json.Value.SerializerOptions, ct).ConfigureAwait(false);
            var created = await rules.CreateAsync(Owner(principal), Draft(request.Name, request.Category, request.Filter, request.Channels, request.Enabled), ct).ConfigureAwait(false);
            http.Response.Headers.ETag = Quote(created.Version);
            var location = $"/api/v1/rules/{created.Id}";
            return Results.Created(location, Map(created));
        }
        catch (Exception e) { return Failure(http, e); }
    }

    private static async Task<IResult> Get(HttpContext http, ClaimsPrincipal principal, string id, RuleUseCases rules, CancellationToken ct)
    {
        try
        {
            var result = await rules.GetAsync(Owner(principal), id, ct).ConfigureAwait(false);
            if (result is null) return Problem(http, 404, "NotFound");
            http.Response.Headers.ETag = Quote(result.Version);
            return Results.Ok(Map(result));
        }
        catch (DomainValidationException) { return Problem(http, 400, "ValidationFailed"); }
    }

    private static async Task<IResult> Replace(HttpContext http, ClaimsPrincipal principal, string id, RuleUseCases rules,
        IOptions<HttpJsonOptions> json, CancellationToken ct)
    {
        try
        {
            var request = await ApiJsonBody.ReadAsync<UpdateRuleRequest>(http.Request, json.Value.SerializerOptions, ct).ConfigureAwait(false);
            if (request.Version is null) return Problem(http, 400, "ValidationFailed");
            var result = await rules.ReplaceAsync(Owner(principal), id,
                Draft(request.Name, request.Category, request.Filter, request.Channels, request.Enabled), request.Version.Value, ct).ConfigureAwait(false);
            http.Response.Headers.ETag = Quote(result.Version);
            return Results.Ok(Map(result));
        }
        catch (Exception e) { return Failure(http, e); }
    }

    private static async Task<IResult> Delete(HttpContext http, ClaimsPrincipal principal, string id, RuleUseCases rules, CancellationToken ct)
    {
        if (!http.Request.Headers.TryGetValue("If-Match", out var values)) return Problem(http, 428, "PreconditionRequired");
        if (values.Count != 1 || !TryStrongVersion(values[0], out var version)) return Problem(http, 400, "ValidationFailed");
        try
        {
            await rules.DeleteAsync(Owner(principal), id, version, ct).ConfigureAwait(false);
            return Results.NoContent();
        }
        catch (Exception e) { return Failure(http, e); }
    }

    private static RuleDraft Draft(string? name, string? categoryText, JsonElement filterElement, string[]? channelValues, bool? enabled)
    {
        if (name is null || categoryText is null || channelValues is null || enabled is null
            || filterElement.ValueKind != JsonValueKind.Object) throw new DomainValidationException("MissingField");
        var category = categoryText switch { "news" => "News", "market" => "Market", "earthquake" => "Earthquake", _ => throw new DomainValidationException("InvalidCategory") };
        var properties = filterElement.EnumerateObject().ToArray();
        string Required(string name)
        {
            var matches = properties.Where(x => x.NameEquals(name)).ToArray();
            if (matches.Length != 1 || matches[0].Value.ValueKind != JsonValueKind.String) throw new DomainValidationException("InvalidFilter");
            return matches[0].Value.GetString()!;
        }
        var allowed = category switch
        {
            "News" => new[] { "keyword" },
            "Market" => new[] { "instrument", "comparisonKind", "thresholdPercent" },
            _ => new[] { "minimumMagnitude", "magnitudeScale" }
        };
        if (properties.Length != allowed.Length || properties.Any(x => !allowed.Contains(x.Name)))
            throw new DomainValidationException("InvalidFilter");
        var filter = category switch
        {
            "News" => new RuleFilterData(category, Keyword: Required("keyword")),
            "Market" => new RuleFilterData(category, Instrument: Required("instrument"), ComparisonKind: Required("comparisonKind"), ThresholdPercent: Required("thresholdPercent")),
            _ => new RuleFilterData(category, MinimumMagnitude: Required("minimumMagnitude"), MagnitudeScale: Required("magnitudeScale"))
        };
        var channels = channelValues.Select(x => x switch { "email" => "Email", "slack" => "Slack", _ => throw new DomainValidationException("InvalidChannels") }).ToArray();
        return new(name, category, filter, channels, enabled.Value);
    }

    private static RuleResponse Map(RuleView x) => new(x.Id, x.Name, x.Category.ToLowerInvariant(),
        new(x.Filter.Keyword, x.Filter.Instrument, x.Filter.ComparisonKind, x.Filter.ThresholdPercent,
            x.Filter.MinimumMagnitude, x.Filter.MagnitudeScale),
        x.Channels.Select(y => y.ToLowerInvariant()).ToArray(), x.Enabled, x.Version,
        Time(x.CreatedAtUtcMs), Time(x.UpdatedAtUtcMs));
    private static string Time(long value) => DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
    private static string Owner(ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static string Quote(long version) => $"\"{version}\"";
    private static string? Single(HttpContext http, string key, string invalidCode = "InvalidQuery")
    {
        if (!http.Request.Query.TryGetValue(key, out var values)) return null;
        if (values.Count != 1 || string.IsNullOrEmpty(values[0])) throw new DomainValidationException(invalidCode);
        return values[0];
    }
    private static bool TryStrongVersion(string? value, out long version)
    {
        version = 0;
        return value is { Length: >= 3 } && value[0] == '"' && value[^1] == '"'
            && long.TryParse(value.AsSpan(1, value.Length - 2), out version) && version > 0;
    }
    private static IResult Failure(HttpContext http, Exception e) => e switch
    {
        DomainValidationException => Problem(http, 400, "ValidationFailed"),
        RuleOperationException { Failure: RuleFailure.NotFound } => Problem(http, 404, "NotFound"),
        RuleOperationException { Failure: RuleFailure.VersionConflict } => Problem(http, 412, "VersionConflict"),
        RuleOperationException { Failure: RuleFailure.RuleLimitReached } => Problem(http, 409, "RuleLimitReached"),
        RuleOperationException { Failure: RuleFailure.ChannelUnavailable } => Problem(http, 400, "ChannelUnavailable"),
        RuleOperationException { Failure: RuleFailure.ImmutableCategory } => Problem(http, 400, "ImmutableCategory"),
        WriterBusyException => Unavailable(http, "WriterBusy"),
        StorageUnavailableException => Unavailable(http, "StorageUnavailable"),
        _ => throw e
    };
    private static IResult Unavailable(HttpContext http, string code)
    {
        http.Response.Headers.RetryAfter = "5";
        return Problem(http, 503, code);
    }
    private static IResult Problem(HttpContext http, int status, string code) => ApiProblems.Create(http, status, code);
}
