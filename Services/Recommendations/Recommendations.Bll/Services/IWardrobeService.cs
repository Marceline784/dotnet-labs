using Recommendations.Bll.Dtos;

namespace Recommendations.Bll.Services;

public interface IWardrobeService
{
    Task<IReadOnlyList<WardrobeItemDto>> GetByUserAsync(long userId, string? slotCode, CancellationToken ct = default);
    Task<WardrobeItemDto> GetAsync(long id, CancellationToken ct = default);
    Task<WardrobeItemDto> UpsertAsync(UpsertWardrobeItemDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<EligibleItemDto>> GetEligibleAsync(long recommendationId, CancellationToken ct = default);
}
