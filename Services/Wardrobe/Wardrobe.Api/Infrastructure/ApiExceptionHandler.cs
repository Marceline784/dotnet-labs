using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Wardrobe.Domain.Exceptions;

namespace Wardrobe.Api.Infrastructure;

/// <summary>
/// Централізована обробка помилок: УСІ винятки перетворюються на ProblemDetails з коректним статусом тут,
/// у контролерах немає try/catch. Кожен виняток логується ОДИН раз (саме тут).
/// EntityNotFound -> 404, Validation -> 400, Conflict / Domain -> 409, решта -> 500 (деталі лише в Development).
/// </summary>
public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ApiExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        if (ex is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
        {
            http.Response.StatusCode = 499;     // клієнт сам перервав запит
            return true;
        }

        var (status, title) = ex switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violation"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        string detail;
        if (status >= 500)
        {
            logger.LogError(ex, "Request {Path} failed", http.Request.Path);
            // у production клієнту не віддаємо внутрішніх деталей
            detail = environment.IsDevelopment()
                ? ex.Message
                : "An unexpected error occurred. Use the correlation id when contacting support.";
        }
        else
        {
            logger.LogWarning("Request {Path} failed with {Status}: {Message}", http.Request.Path, status, ex.Message);
            detail = ex.Message;
        }

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}
