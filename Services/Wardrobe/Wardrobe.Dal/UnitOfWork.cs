using Wardrobe.Dal.Interfaces;
using Wardrobe.Dal.Repositories;

namespace Wardrobe.Dal;

/// <summary>
/// Unit of Work над одним DbContext. Реєструється в DI як Scoped (один екземпляр на HTTP-запит).
/// Єдиний SaveChangesAsync зберігає всі зміни репозиторіїв в одній транзакції.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly WardrobeDbContext _context;

    public UnitOfWork(WardrobeDbContext context)
    {
        _context = context;
        Items = new WardrobeItemRepository(context);
        Categories = new CategoryRepository(context);
        ItemCategories = new ItemCategoryRepository(context);
    }

    public IWardrobeItemRepository Items { get; }
    public ICategoryRepository Categories { get; }
    public IItemCategoryRepository ItemCategories { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => SaveChangesGuard.SaveAsync(_context, ct);
}
