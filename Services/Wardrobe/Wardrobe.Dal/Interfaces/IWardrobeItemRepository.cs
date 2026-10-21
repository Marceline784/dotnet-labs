using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Dal.Interfaces;

public interface IWardrobeItemRepository : IGenericRepository<WardrobeItem>
{
    /// <summary>EAGER LOADING: річ разом із деталями, фото та категоріями ОДНИМ запитом (Include / ThenInclude).</summary>
    Task<WardrobeItem?> GetWithDetailsAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<WardrobeItem>> GetByOwnerAsync(long ownerUserId, Slot? slot = null, CancellationToken ct = default);

    /// <summary>Чи є в користувача жива річ з такою назвою (для перевірки унікальності в сервісі).</summary>
    Task<bool> ExistsByNameAsync(long ownerUserId, string name, long? excludeId = null, CancellationToken ct = default);
}
