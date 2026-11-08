using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

// Окремий простір імен: власні CorrelationIdMiddleware сервісів (Лаб. 02–04) не конфліктують за імʼям
namespace Platform.ServiceDefaults;

/// <summary>
/// Наскрізний CorrelationId (Лаб. 05). Бере X-Correlation-Id із запиту або створює новий (у Gateway це відбувається
/// найчастіше), кладе його в LogContext і в span, повертає клієнту, логує початок і кінець запиту.
/// Формат значення такий самий, як у сервісів (до 64 символів: літери, цифри, '-' і '_').
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    private const string AppliedKey = "Platform.CorrelationId.Applied";

    public async Task InvokeAsync(HttpContext context)
    {
        // ідемпотентність: middleware можна підключити явно (Gateway) і автоматично (ServiceDefaults) без дублів логів
        if (context.Items.ContainsKey(AppliedKey))
        {
            await next(context);
            return;
        }
        context.Items[AppliedKey] = true;

        var path = context.Request.Path;
        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/alive"))
        {
            await next(context);
            return;
        }

        string id = context.Request.Headers[HeaderName].FirstOrDefault() ?? "";
        var generated = false;
        if (id.Length == 0 || id.Length > 64 || !id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'))
        {
            id = Guid.NewGuid().ToString("N");
            context.Request.Headers[HeaderName] = id; // далі його побачать YARP і HttpClient
            generated = true;
        }

        // OnStarting спрацьовує після копіювання відповіді YARP: заголовок у відповіді не дублюється
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        });

        Activity.Current?.SetTag("correlation.id", id);

        using (LogContext.PushProperty("CorrelationId", id))
        {
            if (generated)
                logger.LogInformation("New CorrelationId generated for {Method} {Path}", context.Request.Method, path.Value);

            var sw = Stopwatch.StartNew();
            logger.LogInformation("Request started {Method} {Path}", context.Request.Method, path.Value);
            try
            {
                await next(context);
            }
            finally
            {
                logger.LogInformation("Request finished {Method} {Path} with {StatusCode} in {Elapsed} ms",
                    context.Request.Method, path.Value, context.Response.StatusCode, sw.ElapsedMilliseconds);
            }
        }
    }
}

/// <summary>Додає поточний CorrelationId у вихідні HTTP-запити до інших сервісів.</summary>
public sealed class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var id = accessor.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        if (!string.IsNullOrEmpty(id))
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, id);
        return base.SendAsync(request, ct);
    }
}

internal sealed class CorrelationIdStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        next(app);
    };
}
