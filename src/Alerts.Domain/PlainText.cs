using System.Buffers;
using System.Text;

namespace Alerts.Domain;

/// <summary>Normalizes already-extracted plain text. This is not an HTML parser or output encoder.</summary>
public static class PlainText
{
    public static string Normalize(string value, int maximumScalars, bool allowWhitespaceControls = false)
    {
        Validate.Require(maximumScalars > 0, "InvalidTextLimit");
        ValidateUnicode(value, allowWhitespaceControls);

        var normalized = value.Normalize(NormalizationForm.FormC);
        var result = new StringBuilder();
        var whitespace = false;
        var count = 0;
        foreach (var rune in normalized.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                whitespace = result.Length > 0;
                continue;
            }
            if (whitespace)
            {
                result.Append(' ');
                count++;
                whitespace = false;
            }
            result.Append(rune.ToString());
            count++;
            Validate.Require(count <= maximumScalars, "TextTooLong");
        }
        Validate.Require(count > 0, "EmptyText");
        return result.ToString();
    }

    internal static void ValidateUnicode(string value, bool allowWhitespaceControls = false)
    {
        Validate.Require(value is not null, "InvalidText");
        var remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            Validate.Require(Rune.DecodeFromUtf16(remaining, out var rune, out var consumed) == OperationStatus.Done,
                "InvalidUnicode");
            Validate.Require(!Rune.IsControl(rune) || (allowWhitespaceControls && Rune.IsWhiteSpace(rune)), "TextControlCharacter");
            remaining = remaining[consumed..];
        }

    }

    internal static string? Optional(string? value, int maximumScalars, bool allowWhitespaceControls = false)
        => value is null ? null : Normalize(value, maximumScalars, allowWhitespaceControls);
}
