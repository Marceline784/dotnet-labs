namespace Feedback.Domain.Common;

/// <summary>
/// Value Object: незмінний об'єкт, що порівнюється ЗА ЗНАЧЕННЯМ компонентів, а не за посиланням.
/// (Той самий принцип, що ValueObject у референсному застосунку eShop.)
/// </summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
        => obj is ValueObject other
           && GetType() == other.GetType()
           && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override int GetHashCode()
        => GetEqualityComponents().Aggregate(0, (hash, part) => HashCode.Combine(hash, part));

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
