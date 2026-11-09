using Wardrobe.Bll.Dtos;
using Wardrobe.Bll.Services;

namespace Wardrobe.Bll.Caching;

/// <summary>Довідник категорій читається часто, а змінюється рідко: кешуємо GetAll, запис інвалідує кеш.</summary>
public sealed class CachedCategoryService(CategoryService inner, ICacheService cache) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(CacheKeys.CategoriesScope, ct);
        var result = await cache.GetOrCreateAsync(CacheKeys.Categories(version),
            async c => (IReadOnlyList<CategoryDto>?)await inner.GetAllAsync(c), ct, TimeSpan.FromMinutes(30));   // довідник змінюється рідко
        return result!;
    }

    public Task<CategoryWithItemsDto> GetWithItemsAsync(long id, CancellationToken ct = default)
        => inner.GetWithItemsAsync(id, ct);

    public async Task<CategoryDto> CreateAsync(CategoryInputDto input, CancellationToken ct = default)
    {
        var result = await inner.CreateAsync(input, ct);
        await cache.BumpVersionAsync(CacheKeys.CategoriesScope, "CategoryCreated", ct);
        return result;
    }

    public async Task<CategoryDto> RenameAsync(long id, CategoryInputDto input, CancellationToken ct = default)
    {
        var result = await inner.RenameAsync(id, input, ct);
        await InvalidateAllAsync("CategoryRenamed", ct);   // назва категорії є в картках речей і впливає на фільтр списку
        return result;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await InvalidateAllAsync("CategoryDeleted", ct);
    }

    private async Task InvalidateAllAsync(string reason, CancellationToken ct)
    {
        await cache.BumpVersionAsync(CacheKeys.CategoriesScope, reason, ct);
        await cache.BumpVersionAsync(CacheKeys.ItemsScope, reason, ct);
        await cache.BumpVersionAsync(CacheKeys.ListScope, reason, ct);
    }
}
