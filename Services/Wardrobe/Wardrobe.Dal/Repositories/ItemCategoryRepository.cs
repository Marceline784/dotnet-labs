using Microsoft.EntityFrameworkCore;
using Wardrobe.Dal.Interfaces;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Dal.Repositories;

public class ItemCategoryRepository : GenericRepository<ItemCategory>, IItemCategoryRepository
{
    public ItemCategoryRepository(WardrobeDbContext context) : base(context) { }

    // Складений ключ (ItemId, CategoryId): пошук за одним числовим Id не має сенсу.
    public override Task<ItemCategory?> GetByIdAsync(long id, CancellationToken ct = default)
        => throw new NotSupportedException("ItemCategory has a composite key (ItemId, CategoryId)");

    /// <summary>LINQ to Entities через проміжну сутність: фільтр за категорією та власним полем AddedAt.</summary>
    public async Task<IReadOnlyList<WardrobeItem>> GetRecentlyAddedAsync(string categoryName, DateTime since,
        CancellationToken ct = default)
        => await Set
            .Where(ic => ic.Category.Name == categoryName && ic.AddedAt >= since && !ic.Item.IsDeleted)
            .OrderByDescending(ic => ic.AddedAt)
            .Select(ic => ic.Item)
            .AsNoTracking()
            .ToListAsync(ct);
}
