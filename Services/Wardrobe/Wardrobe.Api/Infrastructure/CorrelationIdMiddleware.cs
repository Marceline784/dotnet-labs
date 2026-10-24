using Serilog.Context;

namespace Wardrobe.Api.Infrastructure;

/// <summary>
/// Кореляційний Id запиту (заголовок X-Correlation-Id або новий) + RequestPath потрапляють у ВСІ структуровані логи
/// запиту як властивості (Serilog LogContext). UserId не додається: автентифікації в сервісі поки немає.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();

        // приймаємо лише безпечне значення, щоб не «засмічувати» логи
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64 ||
            !correlationId.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        {
            await next(context);
        }
    }
}
