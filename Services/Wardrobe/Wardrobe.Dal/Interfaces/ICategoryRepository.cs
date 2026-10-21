using Wardrobe.Domain.Entities;

namespace Wardrobe.Dal.Interfaces;

public interface ICategoryRepository : IGenericRepository<Category>
{
    /// <summary>
    /// EXPLICIT LOADING: категорія завантажується першим запитом, а її речі довантажуються
    /// ОКРЕМИМ другим запитом лише тоді, коли справді потрібні.
    /// </summary>
    Task<Category?> GetWithItemsAsync(long id, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken ct = default);
}
