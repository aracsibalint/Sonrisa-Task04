using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alerts.Api;

internal sealed class ApiRequestException(int status, string code) : Exception(code)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

internal static class ApiJsonBody
{
    public const int MaximumBytes = 65_536;

    public static async Task<T> ReadAsync<T>(HttpRequest request, JsonSerializerOptions options, CancellationToken ct)
    {
        if (request.ContentLength > MaximumBytes) throw new ApiRequestException(413, "PayloadTooLarge");
        var mediaType = request.ContentType?.Split(';', 2)[0].Trim();
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
            throw new ApiRequestException(415, "UnsupportedMediaType");
        if (request.Headers.TryGetValue("Content-Encoding", out var encoding)
            && encoding.Any(x => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, "identity", StringComparison.OrdinalIgnoreCase)))
            throw new ApiRequestException(415, "UnsupportedMediaType");

        await using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var read = await request.Body.ReadAsync(chunk, ct).ConfigureAwait(false);
            if (read == 0) break;
            if (buffer.Length + read > MaximumBytes) throw new ApiRequestException(413, "PayloadTooLarge");
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct).ConfigureAwait(false);
        }

        try
        {
            using var document = JsonDocument.Parse(buffer.GetBuffer().AsMemory(0, checked((int)buffer.Length)),
                new JsonDocumentOptions { MaxDepth = 16 });
            RequireUniqueProperties(document.RootElement);
            return JsonSerializer.Deserialize<T>(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)), options)
                ?? throw new JsonException();
        }
        catch (JsonException) { throw new ApiRequestException(400, "ValidationFailed"); }
    }

    private static void RequireUniqueProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException();
                RequireUniqueProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) RequireUniqueProperties(item);
    }
}

internal static class ApiProblems
{
    public static IResult Create(HttpContext http, int status, string code) => Results.Json(new
    {
        type = $"urn:alerts:problem:{Kebab(code)}",
        title = code switch
        {
            "VersionConflict" => "The rule has changed.",
            "NotFound" => "The resource was not found.",
            "PayloadTooLarge" => "The request payload is too large.",
            "UnsupportedMediaType" => "The request media type is not supported.",
            _ => "The request could not be completed."
        },
        status,
        detail = code switch
        {
            "VersionConflict" => "Read the rule and retry with its current version.",
            "PayloadTooLarge" => "Reduce the request payload and try again.",
            "UnsupportedMediaType" => "Use an uncompressed application/json request.",
            _ => "Correct the request and try again."
        },
        instance = http.Request.Path.Value,
        code,
        traceId = http.TraceIdentifier,
        errors = (object?)null
    }, statusCode: status, contentType: "application/problem+json");

    private static string Kebab(string code) => string.Concat(code.Select((c, i) =>
        char.IsUpper(c) && i > 0 ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
