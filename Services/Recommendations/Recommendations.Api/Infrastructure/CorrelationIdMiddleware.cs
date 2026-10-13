namespace Recommendations.Api.Infrastructure;

/// <summary>
/// Кореляційний ідентифікатор запиту: береться із заголовка X-Correlation-Id або створюється новий,
/// повертається у відповіді, потрапляє в усі логи запиту (через scope) та в ProblemDetails.
/// У логи не пишемо жодних секретів (паролів, токенів): лише технічні Id.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();

        // приймаємо лише безпечне значення (довжина та символи), щоб не «засмічувати» логи
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64 ||
            !correlationId.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
