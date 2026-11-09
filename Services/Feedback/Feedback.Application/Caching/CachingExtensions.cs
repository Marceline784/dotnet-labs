using Feedback.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Feedback.Application.Caching;

public static class CachingExtensions
{
    /// <summary>IMemoryCache з обмеженням розміру + Redis (Aspire) + CachingBehavior у pipeline MediatR.</summary>
    public static TBuilder AddFeedbackCaching<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddMemoryCache(o =>
        {
            o.SizeLimit = 10_000;
            o.CompactionPercentage = 0.2;
            o.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
            o.TrackStatistics = true;
        });
        builder.AddRedisDistributedCache("redis");
        builder.Services.AddSingleton<CacheMetrics>();
        builder.Services.AddSingleton<ICacheService, TwoLevelCache>();

        // реєструється ПІСЛЯ AddApplication: стає найближчим до обробника (усередині Logging і Validation)
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        return builder;
    }
}
