using AutoMapper;
using Microsoft.Extensions.Logging;
using Wardrobe.Bll.Common;
using Wardrobe.Bll.Dtos;
using Wardrobe.Bll.Specifications;
using Wardrobe.Dal.Interfaces;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Enums;
using Wardrobe.Domain.Exceptions;

namespace Wardrobe.Bll.Services;

/// <summary>
/// Бізнес-логіка речей. Працює лише через Unit of Work і репозиторії (EF напряму не використовує).
/// Правила самої сутності (шари, прання, ліміти) перевіряє сутність; сервіс перевіряє правила,
/// для яких потрібні інші дані з БД (наприклад, унікальність назви в гардеробі користувача).
/// </summary>
public sealed class WardrobeItemService(IUnitOfWork uow, IMapper mapper, ILogger<WardrobeItemService> logger)
    : IWardrobeItemService
{
    private const int MaxPageSize = 100;

    // ---------- читання ----------

    public async Task<PagedResult<WardrobeItemListDto>> GetListAsync(WardrobeItemQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        // totalCount рахує БД за ФІЛЬТРОВОЮ специфікацією (без Skip/Take), а сторінку віддає специфікація сторінки
        var total = await uow.Items.CountAsync(new WardrobeItemsFilterSpec(query), ct);
        var items = await uow.Items.ListAsync(new WardrobeItemsPageSpec(query, page, pageSize), ct);

        return new PagedResult<WardrobeItemListDto>(
            mapper.Map<IReadOnlyList<WardrobeItemListDto>>(items), total, page, pageSize);
    }

    public async Task<WardrobeItemDetailsDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        // EAGER LOADING: один запит з усім пов'язаним
        var item = await uow.Items.GetWithDetailsAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(WardrobeItem), id);
        return mapper.Map<WardrobeItemDetailsDto>(item);
    }

    public async Task<IReadOnlyList<WardrobeItemListDto>> GetInLaundryAsync(long ownerUserId, CancellationToken ct = default)
    {
        var items = await uow.Items.ListAsync(new ItemsInLaundrySpec(ownerUserId), ct);
        return mapper.Map<IReadOnlyList<WardrobeItemListDto>>(items);
    }

    public async Task<IReadOnlyList<WardrobeItemListDto>> GetRecentlyAddedAsync(string categoryName, int days, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ValidationException("Category name is required");
        if (days is < 1 or > 365)
            throw new ValidationException("Days must be between 1 and 365");

        var items = await uow.ItemCategories.GetRecentlyAddedAsync(categoryName.Trim(), DateTime.UtcNow.AddDays(-days), ct);
        return mapper.Map<IReadOnlyList<WardrobeItemListDto>>(items);
    }

    // ---------- створення, зміна, видалення ----------

    public async Task<WardrobeItemDetailsDto> CreateAsync(CreateWardrobeItemDto input, CancellationToken ct = default)
    {
        var (slot, layer) = ParseSlotAndLayer(input.Slot, input.Layer);
        var name = input.Name.Trim();

        if (await uow.Items.ExistsByNameAsync(input.OwnerUserId, name, null, ct))
            throw new ConflictException($"An item named '{name}' already exists in this wardrobe");

        var item = new WardrobeItem(input.OwnerUserId, name, slot, layer);   // правила сутності
        if (input.Detail is not null)
            item.SetDetail(ToDetail(input.Detail));

        await uow.Items.AddAsync(item, ct);
        await uow.SaveChangesAsync(ct);                                      // єдиний збереження в кінці операції

        logger.LogInformation("Wardrobe item {ItemId} created for user {OwnerUserId}", item.Id, item.OwnerUserId);
        return await GetByIdAsync(item.Id, ct);
    }

    public async Task<WardrobeItemDetailsDto> UpdateAsync(long id, UpdateWardrobeItemDto input, CancellationToken ct = default)
    {
        var item = await GetTrackedAsync(id, ct);
        var (slot, layer) = ParseSlotAndLayer(input.Slot, input.Layer);
        var name = input.Name.Trim();

        if (await uow.Items.ExistsByNameAsync(item.OwnerUserId, name, id, ct))
            throw new ConflictException($"An item named '{name}' already exists in this wardrobe");

        item.Update(name, slot, layer);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Wardrobe item {ItemId} updated", id);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var item = await GetTrackedAsync(id, ct);
        item.Delete();                                                        // м'яке видалення
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Wardrobe item {ItemId} deleted (soft)", id);
    }

    // ---------- прання ----------

    public async Task<WardrobeItemDetailsDto> MarkInLaundryAsync(long id, CancellationToken ct = default)
    {
        var item = await GetTrackedAsync(id, ct);
        item.MarkInLaundry();                                                 // DomainException -> 409, якщо вже в пранні
        await uow.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<WardrobeItemDetailsDto> ReturnFromLaundryAsync(long id, CancellationToken ct = default)
    {
        var item = await GetTrackedAsync(id, ct);
        item.ReturnFromLaundry();
        await uow.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    // ---------- деталі (1:1) ----------

    public async Task<ItemDetailDto> SetDetailAsync(long id, ItemDetailInputDto input, CancellationToken ct = default)
    {
        var item = await GetAggregateAsync(id, ct);

        if (item.Detail is null)
            item.SetDetail(ToDetail(input));
        else
            item.Detail.Update(input.Warmth, input.MinTemp, input.MaxTemp, input.WaterResistance,
                input.IsWindproof, input.Material, input.Color, input.Notes);

        await uow.SaveChangesAsync(ct);
        return mapper.Map<ItemDetailDto>(item.Detail!);
    }

    // ---------- фото (1:N) ----------

    public async Task<ItemImageDto> AddImageAsync(long id, AddImageDto input, CancellationToken ct = default)
    {
        var item = await GetAggregateAsync(id, ct);
        var image = item.AddImage(input.Url, input.IsPrimary);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<ItemImageDto>(image);
    }

    public async Task SetPrimaryImageAsync(long id, long imageId, CancellationToken ct = default)
    {
        var item = await GetAggregateAsync(id, ct);
        item.SetPrimaryImage(imageId);
        await uow.SaveChangesAsync(ct);
    }

    public async Task RemoveImageAsync(long id, long imageId, CancellationToken ct = default)
    {
        var item = await GetAggregateAsync(id, ct);
        item.RemoveImage(imageId);
        await uow.SaveChangesAsync(ct);
    }

    // ---------- категорії (M:N) ----------

    public async Task<WardrobeItemDetailsDto> AddToCategoryAsync(long id, AddToCategoryDto input, CancellationToken ct = default)
    {
        var item = await GetAggregateAsync(id, ct);
        var category = await uow.Categories.GetByIdAsync(input.CategoryId, ct)
            ?? throw new EntityNotFoundException(nameof(Category), input.CategoryId);

        item.AddToCategory(category);                                         // повтор -> DomainException -> 409
        await uow.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task RemoveFromCategoryAsync(long id, long categoryId, CancellationToken ct = default)
    {
        var item = await GetAggregateAsync(id, ct);
        item.RemoveFromCategory(categoryId);
        await uow.SaveChangesAsync(ct);
    }

    // ---------- допоміжне ----------

    /// <summary>Річ без зв'язків (для простих змін).</summary>
    private async Task<WardrobeItem> GetTrackedAsync(long id, CancellationToken ct)
        => await uow.Items.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException(nameof(WardrobeItem), id);

    /// <summary>Річ з усім агрегатом (потрібно, коли змінюються деталі, фото чи категорії).</summary>
    private async Task<WardrobeItem> GetAggregateAsync(long id, CancellationToken ct)
        => await uow.Items.GetWithDetailsAsync(id, ct) ?? throw new EntityNotFoundException(nameof(WardrobeItem), id);

    private static (Slot Slot, Layer Layer) ParseSlotAndLayer(string slot, string layer)
    {
        if (!EnumParser.TryParse<Slot>(slot, out var s))
            throw new ValidationException($"Slot must be one of: {string.Join(", ", Enum.GetNames<Slot>())}");
        if (!EnumParser.TryParse<Layer>(layer, out var l))
            throw new ValidationException($"Layer must be one of: {string.Join(", ", Enum.GetNames<Layer>())}");
        return (s, l);
    }

    private static ItemDetail ToDetail(ItemDetailInputDto d)
        => new(d.Warmth, d.MinTemp, d.MaxTemp, d.WaterResistance, d.IsWindproof, d.Material, d.Color, d.Notes);
}
