using Wardrobe.Domain.Entities;

namespace Wardrobe.Dal.Interfaces;

public interface IItemCategoryRepository : IGenericRepository<ItemCategory>
{
    /// <summary>
    /// LINQ to Entities через проміжну сутність M:N: речі категорії, додані не раніше вказаної дати.
    /// Використовує власне поле зв'язку AddedAt.
    /// </summary>
    Task<IReadOnlyList<WardrobeItem>> GetRecentlyAddedAsync(string categoryName, DateTime since,
        CancellationToken ct = default);
}
