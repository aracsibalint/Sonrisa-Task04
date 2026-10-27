using System.Diagnostics.CodeAnalysis;

namespace Alerts.Domain;

/// <summary>A stable, transport-independent rejection. Messages never echo submitted values.</summary>
public sealed class DomainValidationException(string code) : ArgumentException(code)
{
    public string Code { get; } = code;
}

internal static class Validate
{
    public static void Require([DoesNotReturnIf(false)] bool condition, string code)
    {
        if (!condition)
        {
            throw new DomainValidationException(code);
        }
    }

    public static Guid Id(Guid value)
    {
        Require(value != Guid.Empty, "InvalidIdentifier");
        return value;
    }
}
