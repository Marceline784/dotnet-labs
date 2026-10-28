using Feedback.Domain.Entities;
using MongoDB.Bson;

namespace Feedback.Domain.Interfaces;

/// <summary>
/// Репозиторій відгуків (шлях ЗАПИСУ). Інтерфейс оголошено в Domain, реалізація буде в Infrastructure (MongoDB).
/// Читання для API виконує окремий інтерфейс IFeedbackQueries (шар Application): це розділення Command/Query.
/// </summary>
public interface IFeedbackRepository
{
    Task<OutfitFeedback?> GetByIdAsync(ObjectId id, CancellationToken ct = default);

    /// <summary>Чи вже залишав цей користувач відгук на цей образ (один відгук від автора на образ).</summary>
    Task<bool> ExistsAsync(long outfitId, long authorUserId, CancellationToken ct = default);

    Task AddAsync(OutfitFeedback feedback, CancellationToken ct = default);

    /// <summary>
    /// Оптимістична конкурентність: оновлення застосовується лише до документа з очікуваною версією,
    /// інакше ConflictException (409).
    /// </summary>
    Task UpdateAsync(OutfitFeedback feedback, CancellationToken ct = default);

    /// <summary>true, якщо документ видалено; false, якщо його не існувало.</summary>
    Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default);
}
