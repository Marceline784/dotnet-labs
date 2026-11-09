using Wardrobe.Bll.Common;
using Wardrobe.Bll.Dtos;
using Wardrobe.Bll.Services;

namespace Wardrobe.Bll.Caching;

/// <summary>
/// Декоратор над WardrobeItemService (Лаб. 03): кеш для читання, інвалідація одразу після запису.
/// Контролери і gRPC-сервіс працюють з IWardrobeItemService, тому кеш діє для обох протоколів без дублювання коду.
/// </summary>
public sealed class CachedWardrobeItemService(WardrobeItemService inner, ICacheService cache) : IWardrobeItemService
{
    // ---------- читання (cache-aside) ----------

    public async Task<WardrobeItemDetailsDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var epoch = await cache.GetVersionAsync(CacheKeys.ItemsScope, ct);
        var dto = await cache.GetOrCreateAsync(CacheKeys.Item(epoch, id),
            async c => (WardrobeItemDetailsDto?)await inner.GetByIdAsync(id, c), ct, TimeSpan.FromMinutes(10));   // не знайдено -> виняток, не кешується
        return dto!;
    }

    public async Task<PagedResult<WardrobeItemListDto>> GetListAsync(WardrobeItemQuery query, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(CacheKeys.ListScope, ct);
        var result = await cache.GetOrCreateAsync(CacheKeys.List(version, query),
            async c => (PagedResult<WardrobeItemListDto>?)await inner.GetListAsync(query, c), ct, TimeSpan.FromMinutes(2));   // списки змінюються частіше
        return result!;
    }

    public async Task<IReadOnlyList<WardrobeItemListDto>> GetInLaundryAsync(long ownerUserId, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(CacheKeys.ListScope, ct);
        var result = await cache.GetOrCreateAsync(CacheKeys.Laundry(version, ownerUserId),
            async c => (IReadOnlyList<WardrobeItemListDto>?)await inner.GetInLaundryAsync(ownerUserId, c), ct, TimeSpan.FromMinutes(2));
        return result!;
    }

    // залежить від поточної дати: не кешується
    public Task<IReadOnlyList<WardrobeItemListDto>> GetRecentlyAddedAsync(string categoryName, int days, CancellationToken ct = default)
        => inner.GetRecentlyAddedAsync(categoryName, days, ct);

    // ---------- запис: після операції інвалідуємо картку речі та списки ----------

    public async Task<WardrobeItemDetailsDto> CreateAsync(CreateWardrobeItemDto input, CancellationToken ct = default)
    {
        var result = await inner.CreateAsync(input, ct);
        await InvalidateAsync(result.Id, "ItemCreated", ct);
        return result;
    }

    public async Task<WardrobeItemDetailsDto> UpdateAsync(long id, UpdateWardrobeItemDto input, CancellationToken ct = default)
    {
        var result = await inner.UpdateAsync(id, input, ct);
        await InvalidateAsync(id, "ItemUpdated", ct);
        return result;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await InvalidateAsync(id, "ItemDeleted", ct);
    }

    public async Task<WardrobeItemDetailsDto> MarkInLaundryAsync(long id, CancellationToken ct = default)
    {
        var result = await inner.MarkInLaundryAsync(id, ct);
        await InvalidateAsync(id, "ItemSentToLaundry", ct);
        return result;
    }

    public async Task<WardrobeItemDetailsDto> ReturnFromLaundryAsync(long id, CancellationToken ct = default)
    {
        var result = await inner.ReturnFromLaundryAsync(id, ct);
        await InvalidateAsync(id, "ItemReturnedFromLaundry", ct);
        return result;
    }

    public async Task<ItemDetailDto> SetDetailAsync(long id, ItemDetailInputDto input, CancellationToken ct = default)
    {
        var result = await inner.SetDetailAsync(id, input, ct);
        await InvalidateAsync(id, "ItemDetailChanged", ct);
        return result;
    }

    public async Task<ItemImageDto> AddImageAsync(long id, AddImageDto input, CancellationToken ct = default)
    {
        var result = await inner.AddImageAsync(id, input, ct);
        await InvalidateAsync(id, "ImageAdded", ct);
        return result;
    }

    public async Task SetPrimaryImageAsync(long id, long imageId, CancellationToken ct = default)
    {
        await inner.SetPrimaryImageAsync(id, imageId, ct);
        await InvalidateAsync(id, "PrimaryImageChanged", ct);
    }

    public async Task RemoveImageAsync(long id, long imageId, CancellationToken ct = default)
    {
        await inner.RemoveImageAsync(id, imageId, ct);
        await InvalidateAsync(id, "ImageRemoved", ct);
    }

    public async Task<WardrobeItemDetailsDto> AddToCategoryAsync(long id, AddToCategoryDto input, CancellationToken ct = default)
    {
        var result = await inner.AddToCategoryAsync(id, input, ct);
        await InvalidateAsync(id, "ItemAddedToCategory", ct);
        return result;
    }

    public async Task RemoveFromCategoryAsync(long id, long categoryId, CancellationToken ct = default)
    {
        await inner.RemoveFromCategoryAsync(id, categoryId, ct);
        await InvalidateAsync(id, "ItemRemovedFromCategory", ct);
    }

    private async Task InvalidateAsync(long itemId, string reason, CancellationToken ct)
    {
        var epoch = await cache.GetVersionAsync(CacheKeys.ItemsScope, ct);
        await cache.RemoveAsync(CacheKeys.Item(epoch, itemId), reason, ct);
        await cache.BumpVersionAsync(CacheKeys.ListScope, reason, ct);
    }
}
