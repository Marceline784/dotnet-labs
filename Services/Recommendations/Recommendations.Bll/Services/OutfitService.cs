using AutoMapper;
using Microsoft.Extensions.Logging;
using Recommendations.Bll.Dtos;
using Recommendations.Dal.Interfaces;
using Recommendations.Domain.Constants;
using Recommendations.Domain.Exceptions;

namespace Recommendations.Bll.Services;

public sealed class OutfitService(IUnitOfWork uow, IMapper mapper, ILogger<OutfitService> logger) : IOutfitService
{
    public async Task<OutfitDto> GetAsync(long id, CancellationToken ct = default)
    {
        var outfit = await uow.Outfits.GetWithItemsAsync(id, ct)
            ?? throw new NotFoundException($"Outfit {id} not found");
        return mapper.Map<OutfitDto>(outfit);
    }

    /// <summary>
    /// Допустимість переходу перевіряє процедура usp_ChangeOutfitStatus (одна в БД, обійти її неможливо).
    /// Тут лише перевіряється, що назва статусу взагалі існує.
    /// </summary>
    public async Task<OutfitDto> ChangeStatusAsync(long id, ChangeOutfitStatusDto input, CancellationToken ct = default)
    {
        if (!OutfitStatuses.All.Contains(input.Status))
            throw new ValidationException($"Status must be one of: {string.Join(", ", OutfitStatuses.All)}");

        await uow.Outfits.ChangeStatusAsync(id, input.Status, "api", input.Note, ct);   // 404/409 приходять винятками з DAL
        logger.LogInformation("Outfit {OutfitId} status changed to {Status}", id, input.Status);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        if (!await uow.Outfits.DeleteAsync(id, ct))
            throw new NotFoundException($"Outfit {id} not found");
    }
}
