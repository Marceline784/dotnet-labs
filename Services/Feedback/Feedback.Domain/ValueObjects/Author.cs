using Feedback.Domain.Common;
using Feedback.Domain.Exceptions;

namespace Feedback.Domain.ValueObjects;

/// <summary>Автор відгуку (вкладений документ author). Id користувача належить іншому сервісу.</summary>
public sealed class Author : ValueObject
{
    public const int DisplayNameMaxLength = 100;

    public long UserId { get; }
    public string DisplayName { get; }

    public Author(long userId, string displayName)
    {
        if (userId <= 0)
            throw new DomainException("Author user id must be positive");

        UserId = userId;
        DisplayName = Guard.Required(displayName, "DisplayName", DisplayNameMaxLength);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return DisplayName;
    }
}
