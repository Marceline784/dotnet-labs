using Feedback.Application.Common.Models;
using Feedback.Application.Feedbacks.Dtos;

namespace Feedback.Application.Common.Interfaces;

/// <summary>
/// Шлях ЧИТАННЯ. Обробники запитів звертаються до цього інтерфейсу, а не до MongoDB напряму
/// (Application не залежить від Infrastructure). Реалізація в Infrastructure використовує проєкції та
/// aggregation pipeline, а пагінацію й фільтрацію виконує MongoDB (skip/limit, $match), а не пам'ять.
/// </summary>
public interface IFeedbackQueries
{
    Task<FeedbackDetailsDto?> GetByIdAsync(string id, CancellationToken ct);

    Task<PagedResult<FeedbackDto>> GetListAsync(
        long? outfitId, long? authorUserId, string? feeling, string? search,
        int page, int pageSize, CancellationToken ct);
}
