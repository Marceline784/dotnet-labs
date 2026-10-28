using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Feedback.Application.Common.Behaviors;

/// <summary>
/// Структуроване логування КОЖНОГО запиту: що прийшло, скільки тривало, чим закінчилось.
/// Стоїть найзовнішим у pipeline, тому бачить і запити, відхилені валідацією: для них після
/// «Handling» одразу йде «failed», а рядків самого обробника в лозі немає (обробник не виконувався).
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} {@Request}", name, request);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName} in {ElapsedMs} ms", name, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            // стек і HTTP-статус обробляє глобальний обробник помилок у WebApi; тут лише факт і причина
            logger.LogWarning("Request {RequestName} failed in {ElapsedMs} ms: {ErrorType}: {ErrorMessage}",
                name, stopwatch.ElapsedMilliseconds, ex.GetType().Name, ex.Message);
            throw;
        }
    }
}
