using Feedback.Domain.Exceptions;

namespace Feedback.Domain.Common;

/// <summary>Малі перевірки для конструкторів і методів доменних типів: порушення дають DomainException.</summary>
internal static class Guard
{
    public static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} is too long (max {maxLength})");
        return trimmed;
    }

    public static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} is too long (max {maxLength})");
        return trimmed;
    }
}
