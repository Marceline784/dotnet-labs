namespace Wardrobe.Domain.Entities;

/// <summary>
/// Проміжна сутність M:N між річчю та категорією зі ВЛАСНИМ полем AddedAt (дата додавання в категорію).
/// Автоматичне M:N в EF Core такого поля зберегти не дозволило б.
/// </summary>
public class ItemCategory
{
    public long ItemId { get; private set; }
    public WardrobeItem Item { get; private set; } = null!;
    public long CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public DateTime AddedAt { get; private set; }

    private ItemCategory() { }                          // для EF Core

    internal ItemCategory(WardrobeItem item, Category category)
    {
        Item = item;
        Category = category;
        AddedAt = DateTime.UtcNow;
    }
}
