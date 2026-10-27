using System.Text.Json;
using Alerts.Domain;

namespace Alerts.Infrastructure.Persistence;

internal static class FilterSnapshot
{
    internal static void RequireRuleEvidence(RuleRow rule, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var agrees = rule.Category switch
        {
            "News" => root.GetProperty("keyword").GetString() == rule.Keyword,
            "Market" => root.GetProperty("thresholdPercent").GetString() == rule.ThresholdPercentText
                && root.GetProperty("instrument").GetString() == rule.Instrument && root.GetProperty("comparisonKind").GetString() == rule.ComparisonKind,
            "Earthquake" => root.GetProperty("minimumMagnitude").GetString() == rule.MinimumMagnitudeText
                && root.GetProperty("magnitudeScale").GetString() == rule.MagnitudeScale,
            _ => false
        };
        if (!agrees) throw new InvalidOperationException("Filter snapshot differs from the referenced rule version.");
    }

    internal static void Validate(string category, string json)
    {
        if (json.Length is < 1 or > 2048) throw new InvalidOperationException("Invalid filter snapshot size.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Invalid filter snapshot.");
        string[] expected = category switch
        {
            "News" => ["schemaVersion", "keyword"],
            "Market" => ["schemaVersion", "instrument", "comparisonKind", "thresholdPercent"],
            "Earthquake" => ["schemaVersion", "minimumMagnitude", "magnitudeScale"],
            _ => throw new InvalidOperationException("Invalid snapshot category.")
        };
        var fields = root.EnumerateObject().Select(p => p.Name).ToArray();
        if (!fields.Order(StringComparer.Ordinal).SequenceEqual(expected.Order(StringComparer.Ordinal))
            || !root.GetProperty("schemaVersion").TryGetInt32(out var version) || version != 1)
            throw new InvalidOperationException("Unknown/duplicate snapshot fields or schema version.");
        string Text(string field) => root.GetProperty(field).GetString() ?? throw new InvalidOperationException("Null filter value.");
        switch (category)
        {
            case "News":
                var keyword = Text("keyword");
                if (PlainText.Normalize(keyword, 100) != keyword) throw new InvalidOperationException("Noncanonical snapshot keyword.");
                break;
            case "Market":
                if (Text("instrument") != "BTCUSDT" || Text("comparisonKind") != "binance-rolling-24h"
                    || CanonicalDecimal.Parse(Text("thresholdPercent"), DecimalKind.PercentThreshold).Text != Text("thresholdPercent"))
                    throw new InvalidOperationException("Invalid market snapshot.");
                break;
            case "Earthquake":
                if (Text("magnitudeScale") != "Mw" || CanonicalDecimal.Parse(Text("minimumMagnitude"), DecimalKind.Magnitude).Text != Text("minimumMagnitude"))
                    throw new InvalidOperationException("Invalid earthquake snapshot.");
                break;
        }
    }
}
