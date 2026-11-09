using Feedback.Application.Caching;
using MediatR;

namespace Feedback.Application.Common.Behaviors;

/// <summary>
/// Cache-aside для запитів із маркером ICacheableQuery та інвалідація після команд із маркером
/// ICacheInvalidatingCommand. Обробники й валідатори Лаб. 04 не змінюються.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(ICacheService cache)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is ICacheableQuery query)
        {
            var version = await cache.GetVersionAsync(CacheKeys.Scope, ct);
            var cached = await cache.GetOrCreateAsync<TResponse>(
                $"{CacheKeys.Scope}:v{version}:{query.CacheKey}",
                async c => await next(c), ct, query.Ttl);
            return cached!;
        }

        var response = await next(ct);

        if (request is ICacheInvalidatingCommand)
            await cache.BumpVersionAsync(CacheKeys.Scope, $"{typeof(TRequest).Name} executed", ct);   // одразу після запису

        return response;
    }
}
