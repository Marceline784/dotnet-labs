using Microsoft.EntityFrameworkCore;
using Wardrobe.Dal.Interfaces;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Dal.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(WardrobeDbContext context) : base(context) { }

    /// <summary>EXPLICIT LOADING: два окремі запити (категорія, потім її речі).</summary>
    public async Task<Category?> GetWithItemsAsync(long id, CancellationToken ct = default)
    {
        var category = await Set.FirstOrDefaultAsync(c => c.Id == id, ct);      // 1-й запит
        if (category is null) return null;

        await Context.Entry(category)
            .Collection(c => c.ItemCategories)
            .Query()
            .Include(ic => ic.Item)
            .Where(ic => !ic.Item.IsDeleted)
            .LoadAsync(ct);                                                    // 2-й запит

        return category;
    }

    public Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken ct = default)
        => Set.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId), ct);
}
