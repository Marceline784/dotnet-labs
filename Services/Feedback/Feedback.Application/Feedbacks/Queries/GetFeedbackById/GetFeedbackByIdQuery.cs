using Feedback.Application.Caching;
using Feedback.Application.Common.Interfaces;
using Feedback.Application.Feedbacks.Dtos;

namespace Feedback.Application.Feedbacks.Queries.GetFeedbackById;

public record GetFeedbackByIdQuery(string Id) : IQuery<FeedbackDetailsDto>, ICacheableQuery
{
    public string CacheKey => $"id:{Id}";
    public TimeSpan Ttl => TimeSpan.FromMinutes(10);   // картка відгуку змінюється рідко
}
