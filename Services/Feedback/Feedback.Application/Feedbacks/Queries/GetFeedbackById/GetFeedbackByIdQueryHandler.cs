using Feedback.Application.Common.Interfaces;
using Feedback.Application.Feedbacks.Dtos;
using Feedback.Domain.Exceptions;
using MediatR;

namespace Feedback.Application.Feedbacks.Queries.GetFeedbackById;

/// <summary>Обробник запиту не знає про MongoDB: він викликає IFeedbackQueries.</summary>
public class GetFeedbackByIdQueryHandler(IFeedbackQueries queries)
    : IRequestHandler<GetFeedbackByIdQuery, FeedbackDetailsDto>
{
    public async Task<FeedbackDetailsDto> Handle(GetFeedbackByIdQuery query, CancellationToken ct)
        => await queries.GetByIdAsync(query.Id, ct) ?? throw new NotFoundException("Feedback", query.Id);
}
