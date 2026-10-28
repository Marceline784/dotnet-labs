using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Feedback.Application.Common.Behaviors;

/// <summary>Вимірює час виконання обробника (разом із запитами до MongoDB) і попереджає про аномально довгі.</summary>
public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private const long SlowThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowThresholdMs)
            logger.LogWarning("Slow request {RequestName}: {ElapsedMs} ms", typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
