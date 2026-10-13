using Recommendations.Domain.Models;

namespace Recommendations.Dal.Interfaces;

public interface IRecommendationRepository : IGenericRepository<Recommendation>
{
    /// <summary>Рекомендація разом зі знімком погоди (без образів).</summary>
    Task<Recommendation?> GetWithWeatherAsync(long id, CancellationToken ct = default);

    /// <summary>Історія рекомендацій користувача зі сторінковістю, найновіші першими.</summary>
    Task<IReadOnlyList<Recommendation>> GetByUserAsync(long userId, int skip, int take, CancellationToken ct = default);
}
