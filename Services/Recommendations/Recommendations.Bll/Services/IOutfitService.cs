using Recommendations.Bll.Dtos;

namespace Recommendations.Bll.Services;

public interface IOutfitService
{
    Task<OutfitDto> GetAsync(long id, CancellationToken ct = default);
    Task<OutfitDto> ChangeStatusAsync(long id, ChangeOutfitStatusDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
