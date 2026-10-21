using Microsoft.EntityFrameworkCore;
using Wardrobe.Dal.Interfaces;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Dal.Repositories;

public class WardrobeItemRepository : GenericRepository<WardrobeItem>, IWardrobeItemRepository
{
    public WardrobeItemRepository(WardrobeDbContext context) : base(context) { }

    // м'яко видалені речі (IsDeleted) для решти коду «не існують»
    public override async Task<WardrobeItem?> GetByIdAsync(long id, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct);

    public override async Task<IReadOnlyList<WardrobeItem>> GetAllAsync(CancellationToken ct = default)
        => await Set.AsNoTracking().Where(i => !i.IsDeleted).ToListAsync(ct);

    /// <summary>EAGER LOADING: один запит із JOIN-ами для всього агрегату.</summary>
    public Task<WardrobeItem?> GetWithDetailsAsync(long id, CancellationToken ct = default)
        => Set.Include(i => i.Detail)
              .Include(i => i.Images)
              .Include(i => i.ItemCategories).ThenInclude(ic => ic.Category)
              .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct);

    public async Task<IReadOnlyList<WardrobeItem>> GetByOwnerAsync(long ownerUserId, Slot? slot = null,
        CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Where(i => i.OwnerUserId == ownerUserId && !i.IsDeleted);
        if (slot is not null)
            query = query.Where(i => i.Slot == slot);

        return await query.OrderBy(i => i.Slot).ThenBy(i => i.Layer).ThenBy(i => i.Name).ToListAsync(ct);
    }

    public Task<bool> ExistsByNameAsync(long ownerUserId, string name, long? excludeId = null,
        CancellationToken ct = default)
        => Set.AnyAsync(i => i.OwnerUserId == ownerUserId && !i.IsDeleted
                             && i.Name == name && (excludeId == null || i.Id != excludeId), ct);
}
