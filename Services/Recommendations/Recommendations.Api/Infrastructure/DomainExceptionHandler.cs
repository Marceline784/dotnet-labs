using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Recommendations.Domain.Exceptions;

namespace Recommendations.Api.Infrastructure;

/// <summary>
/// ЄДИНЕ місце, де винятки перетворюються на HTTP-статуси (контролери без try/catch).
/// Ланцюжок: процедура БД (THROW) -> DAL (SqlExceptionTranslator) -> BLL -> цей обробник -> статус у форматі ProblemDetails.
/// </summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        int status;
        string title;
        string detail;

        switch (ex)
        {
            case NotFoundException:
                (status, title, detail) = (StatusCodes.Status404NotFound, "Resource not found", ex.Message);
                break;
            case BusinessConflictException:
                (status, title, detail) = (StatusCodes.Status409Conflict, "Business rule conflict", ex.Message);
                break;
            case ValidationException:
                (status, title, detail) = (StatusCodes.Status400BadRequest, "Validation error", ex.Message);
                break;
            case ExternalServiceException:
                (status, title, detail) = (StatusCodes.Status503ServiceUnavailable, "External service unavailable", ex.Message);
                break;
            case OperationCanceledException when http.RequestAborted.IsCancellationRequested:
                http.Response.StatusCode = 499;     // клієнт сам перервав запит: відповідь уже нікому не потрібна
                return true;
            default:
                logger.LogError(ex, "Unhandled exception");
                // деталі помилки клієнту НЕ віддаємо (там можуть бути внутрішні дані)
                (status, title, detail) = (StatusCodes.Status500InternalServerError, "Unexpected error",
                    "An unexpected error occurred. Use the correlation id when contacting support.");
                break;
        }

        if (status < 500)
            logger.LogWarning("Request failed with {Status}: {Message}", status, ex.Message);

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}
