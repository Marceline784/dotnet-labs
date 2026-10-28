using Feedback.Domain.Common;
using Feedback.Domain.Exceptions;

namespace Feedback.Domain.ValueObjects;

/// <summary>
/// Знімок речі з образу на момент відгуку (денормалізація). Не оновлюється, навіть якщо річ у «Гардеробі» зміниться.
/// </summary>
public sealed class ItemSnapshot : ValueObject
{
    public const int NameMaxLength = 200;
    private static readonly string[] Slots = { "Top", "Bottom", "Shoes", "Bag", "Accessory" };

    public long ItemId { get; }
    public string Name { get; }
    public string? Slot { get; }

    public ItemSnapshot(long itemId, string name, string? slot = null)
    {
        if (itemId <= 0)
            throw new DomainException("Item id must be positive");
        if (slot is not null && !Slots.Contains(slot))
            throw new DomainException($"Slot must be one of: {string.Join(", ", Slots)}");

        ItemId = itemId;
        Name = Guard.Required(name, "Item name", NameMaxLength);
        Slot = slot;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ItemId;
        yield return Name;
        yield return Slot;
    }
}
