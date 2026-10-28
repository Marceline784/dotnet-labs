using Feedback.Application.Common.Interfaces;
using Feedback.Application.Feedbacks.Dtos;

namespace Feedback.Application.Feedbacks.Queries.GetFeedbackById;

public record GetFeedbackByIdQuery(string Id) : IQuery<FeedbackDetailsDto>;
