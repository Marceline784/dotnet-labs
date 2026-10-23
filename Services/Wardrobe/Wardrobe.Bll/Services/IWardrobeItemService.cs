using Wardrobe.Bll.Common;
using Wardrobe.Bll.Dtos;

namespace Wardrobe.Bll.Services;

public interface IWardrobeItemService
{
    Task<PagedResult<WardrobeItemListDto>> GetListAsync(WardrobeItemQuery query, CancellationToken ct = default);
    Task<WardrobeItemDetailsDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<WardrobeItemDetailsDto> CreateAsync(CreateWardrobeItemDto input, CancellationToken ct = default);
    Task<WardrobeItemDetailsDto> UpdateAsync(long id, UpdateWardrobeItemDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);

    Task<WardrobeItemDetailsDto> MarkInLaundryAsync(long id, CancellationToken ct = default);
    Task<WardrobeItemDetailsDto> ReturnFromLaundryAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<WardrobeItemListDto>> GetInLaundryAsync(long ownerUserId, CancellationToken ct = default);

    Task<ItemDetailDto> SetDetailAsync(long id, ItemDetailInputDto input, CancellationToken ct = default);
    Task<ItemImageDto> AddImageAsync(long id, AddImageDto input, CancellationToken ct = default);
    Task SetPrimaryImageAsync(long id, long imageId, CancellationToken ct = default);
    Task RemoveImageAsync(long id, long imageId, CancellationToken ct = default);

    Task<WardrobeItemDetailsDto> AddToCategoryAsync(long id, AddToCategoryDto input, CancellationToken ct = default);
    Task RemoveFromCategoryAsync(long id, long categoryId, CancellationToken ct = default);

    /// <summary>Речі категорії, додані за останні N днів (M:N через проміжну сутність).</summary>
    Task<IReadOnlyList<WardrobeItemListDto>> GetRecentlyAddedAsync(string categoryName, int days, CancellationToken ct = default);
}
