using Recommendations.Domain.Models;

namespace Recommendations.Dal.Interfaces;

public interface IOutfitRepository : IGenericRepository<Outfit>
{
    /// <summary>Образ разом із позиціями (один запит із multi-mapping).</summary>
    Task<Outfit?> GetWithItemsAsync(long outfitId, CancellationToken ct = default);

    /// <summary>Усі образи рекомендації (карусель) із позиціями, за порядком у каруселі.</summary>
    Task<IReadOnlyList<Outfit>> GetByRecommendationAsync(long recommendationId, CancellationToken ct = default);

    /// <summary>Зміна статусу через usp_ChangeOutfitStatus (перевірка переходу та запис в історію робить БД).</summary>
    Task ChangeStatusAsync(long outfitId, string newStatus, string? changedBy = null, string? note = null,
        CancellationToken ct = default);

    /// <summary>
    /// Відбитки (ItemsHash) образів, які користувач одягав (Worn/Reviewed) за останні <paramref name="days"/> днів
    /// до <paramref name="forDate"/>. Потрібні, щоб не пропонувати повтор.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetRecentlyWornHashesAsync(long userId, DateTime forDate, int days,
        CancellationToken ct = default);
}
