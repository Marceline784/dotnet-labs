namespace Wardrobe.Dal.Interfaces;

/// <summary>
/// Unit of Work: набір репозиторіїв над ОДНИМ контекстом і єдиний SaveChangesAsync.
/// Репозиторії лише додають/змінюють/видаляють сутності в контексті; усі зміни потрапляють у БД
/// одним викликом SaveChangesAsync, тобто в одній транзакції.
/// </summary>
public interface IUnitOfWork
{
    IWardrobeItemRepository Items { get; }
    ICategoryRepository Categories { get; }
    IItemCategoryRepository ItemCategories { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
