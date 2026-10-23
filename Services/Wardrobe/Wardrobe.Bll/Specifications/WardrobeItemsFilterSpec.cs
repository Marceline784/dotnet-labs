using Ardalis.Specification;
using Wardrobe.Bll.Dtos;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Bll.Specifications;

/// <summary>
/// Специфікація-ФІЛЬТР: лише критерії вибірки (Where), без сортування й пагінації.
/// Саме її використовуємо для totalCount: рахуються ВСІ записи за фільтром, а не лише поточна сторінка.
/// Специфікація не залежить від EF, тож її можна тестувати окремо.
/// </summary>
public class WardrobeItemsFilterSpec : Specification<WardrobeItem>
{
    public WardrobeItemsFilterSpec(WardrobeItemQuery q)
    {
        Query.Where(i => !i.IsDeleted);

        if (q.OwnerUserId is not null)
            Query.Where(i => i.OwnerUserId == q.OwnerUserId);

        if (!string.IsNullOrWhiteSpace(q.Search))
            Query.Where(i => i.Name.Contains(q.Search));

        if (q.Slot is not null)
            Query.Where(i => i.Slot == q.Slot);

        if (q.IsAvailable is not null)
            Query.Where(i => i.IsAvailable == q.IsAvailable);

        if (!string.IsNullOrWhiteSpace(q.Category))
            Query.Where(i => i.ItemCategories.Any(ic => ic.Category.Name == q.Category));

        if (q.MinWarmth is not null)
            Query.Where(i => i.Detail != null && i.Detail.Warmth >= q.MinWarmth);
    }
}
