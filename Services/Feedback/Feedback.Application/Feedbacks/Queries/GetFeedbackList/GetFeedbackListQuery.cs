using Feedback.Application.Common.Interfaces;
using Feedback.Application.Common.Models;
using Feedback.Application.Feedbacks.Dtos;

namespace Feedback.Application.Feedbacks.Queries.GetFeedbackList;

/// <summary>Список відгуків: фільтри, повнотекстовий пошук (text-індекс) і пагінація виконуються в MongoDB.</summary>
public record GetFeedbackListQuery(
    long? OutfitId = null,
    long? AuthorUserId = null,
    string? Feeling = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<FeedbackDto>>;
