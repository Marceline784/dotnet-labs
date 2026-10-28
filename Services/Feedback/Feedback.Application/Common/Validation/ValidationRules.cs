using Feedback.Domain.Enums;
using Feedback.Domain.Exceptions;
using MongoDB.Bson;

namespace Feedback.Application.Common.Validation;

/// <summary>MongoDB-специфічні й спільні перевірки для валідаторів.</summary>
public static class ObjectIdRules
{
    /// <summary>Рядок має бути коректним ObjectId (24 hex-символи).</summary>
    public static bool IsValid(string? id) => !string.IsNullOrWhiteSpace(id) && ObjectId.TryParse(id, out _);
}

public static class FeelingParser
{
    public static bool IsValid(string? value) => TryParse(value, out _);

    public static bool TryParse(string? value, out Feeling feeling)
    {
        feeling = default;
        return !string.IsNullOrWhiteSpace(value)
               && !int.TryParse(value, out _)
               && Enum.TryParse(value, ignoreCase: true, out feeling)
               && Enum.IsDefined(feeling);
    }

    public static Feeling Parse(string? value)
        => TryParse(value, out var feeling)
            ? feeling
            : throw new DomainException($"Feeling must be one of: {string.Join(", ", Enum.GetNames<Feeling>())}");
}

/// <summary>Захист від втрати одночасних змін: клієнт передає версію, яку бачив (ETag / If-Match).</summary>
public static class VersionGuard
{
    public static void Ensure(int? expected, int actual, string id)
    {
        if (expected is not null && expected != actual)
            throw new ConflictException(
                $"Feedback {id} was modified by someone else (expected version {expected}, current {actual})");
    }
}
