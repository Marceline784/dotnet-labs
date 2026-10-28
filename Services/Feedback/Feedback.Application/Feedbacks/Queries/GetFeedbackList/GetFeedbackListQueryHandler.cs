using Feedback.Application.Common.Interfaces;
using Feedback.Application.Common.Models;
using Feedback.Application.Feedbacks.Dtos;
using MediatR;

namespace Feedback.Application.Feedbacks.Queries.GetFeedbackList;

public class GetFeedbackListQueryHandler(IFeedbackQueries queries)
    : IRequestHandler<GetFeedbackListQuery, PagedResult<FeedbackDto>>
{
    public Task<PagedResult<FeedbackDto>> Handle(GetFeedbackListQuery q, CancellationToken ct)
        => queries.GetListAsync(
            q.OutfitId, q.AuthorUserId, q.Feeling, q.Search,
            Math.Max(q.Page, 1), Math.Clamp(q.PageSize, 1, 100), ct);
}
