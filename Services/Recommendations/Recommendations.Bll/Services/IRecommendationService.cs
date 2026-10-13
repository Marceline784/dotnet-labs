using Recommendations.Bll.Dtos;

namespace Recommendations.Bll.Services;

public interface IRecommendationService
{
    /// <summary>Погода (автоматично) -> підбір речей -> образи -> збереження в одній транзакції.</summary>
    Task<RecommendationDto> CreateAsync(CreateRecommendationDto input, CancellationToken ct = default);
    Task<RecommendationDto> GetAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<RecommendationSummaryDto>> GetByUserAsync(long userId, int page, int pageSize, CancellationToken ct = default);
}
