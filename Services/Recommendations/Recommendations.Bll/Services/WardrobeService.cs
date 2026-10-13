using AutoMapper;
using Microsoft.Extensions.Logging;
using Recommendations.Bll.Dtos;
using Recommendations.Dal.Interfaces;
using Recommendations.Domain.Constants;
using Recommendations.Domain.Exceptions;
using Recommendations.Domain.Models;

namespace Recommendations.Bll.Services;

public sealed class WardrobeService(IUnitOfWork uow, IMapper mapper, ILogger<WardrobeService> logger) : IWardrobeService
{
    private static readonly string[] Layers = { "Base", "Mid", "Outer" };

    public async Task<IReadOnlyList<WardrobeItemDto>> GetByUserAsync(long userId, string? slotCode, CancellationToken ct = default)
    {
        if (slotCode is not null && !SlotCodes.All.Contains(slotCode))
            throw new ValidationException($"Slot must be one of: {string.Join(", ", SlotCodes.All)}");
        if (await uow.Users.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException($"User {userId} not found");

        return mapper.Map<IReadOnlyList<WardrobeItemDto>>(await uow.WardrobeItems.GetByOwnerAsync(userId, slotCode, ct));
    }

    public async Task<WardrobeItemDto> GetAsync(long id, CancellationToken ct = default)
    {
        var item = await uow.WardrobeItems.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Wardrobe item {id} not found");
        return mapper.Map<WardrobeItemDto>(item);
    }

    public async Task<WardrobeItemDto> UpsertAsync(UpsertWardrobeItemDto input, CancellationToken ct = default)
    {
        // бізнес-валідація: правила, які не виразити атрибутами
        if (!SlotCodes.All.Contains(input.SlotCode))
            throw new ValidationException($"Slot must be one of: {string.Join(", ", SlotCodes.All)}");
        if (!Layers.Contains(input.Layer))
            throw new ValidationException($"Layer must be one of: {string.Join(", ", Layers)}");
        if (input.SlotCode != SlotCodes.Top && input.Layer != "Base")
            throw new ValidationException("Layers other than Base are allowed only for the Top slot");
        if (input.MaxTemp <= input.MinTemp)
            throw new ValidationException("MaxTemp must be greater than MinTemp");

        if (await uow.Users.GetByIdAsync(input.OwnerUserId, ct) is null)
            throw new NotFoundException($"User {input.OwnerUserId} not found");

        var item = new WardrobeItem
        {
            Id = input.Id,
            OwnerUserId = input.OwnerUserId,
            Name = input.Name.Trim(),
            SlotCode = input.SlotCode,
            Layer = input.Layer,
            Warmth = input.Warmth,
            MinTemp = input.MinTemp,
            MaxTemp = input.MaxTemp,
            WaterResistance = input.WaterResistance,
            IsAvailable = input.IsAvailable,
            PrimaryImageUrl = input.PrimaryImageUrl,
            SourceUpdatedAt = DateTime.UtcNow
        };
        await uow.WardrobeItems.AddAsync(item, ct);

        logger.LogInformation("Wardrobe item {ItemId} upserted for user {UserId}", item.Id, item.OwnerUserId);
        return await GetAsync(item.Id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        if (!await uow.WardrobeItems.DeleteAsync(id, ct))
            throw new NotFoundException($"Wardrobe item {id} not found");
    }

    public async Task<IReadOnlyList<EligibleItemDto>> GetEligibleAsync(long recommendationId, CancellationToken ct = default) =>
        mapper.Map<IReadOnlyList<EligibleItemDto>>(await uow.WardrobeItems.GetEligibleAsync(recommendationId, ct));
}
