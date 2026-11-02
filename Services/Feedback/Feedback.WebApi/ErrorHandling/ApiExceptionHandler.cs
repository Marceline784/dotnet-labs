using Feedback.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Feedback.WebApi.ErrorHandling;

/// <summary>
/// Централізована обробка помилок (IExceptionHandler + ProblemDetails), включно з MongoDB-специфічними:
/// NotFound -> 404, ValidationException -> 400 (усі помилки разом), DomainException -> 400,
/// ConflictException (конфлікт версій) -> 409, MongoWriteException з DuplicateKey -> 409 (інші помилки запису -> 400),
/// MongoConnectionException / таймаут вибору сервера -> 503, решта -> 500 (деталі лише в Development).
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
            http.Response.StatusCode = 499;      // клієнт сам перервав запит
            return true;
        }

        ProblemDetails body = ex switch
        {
            ValidationException v => ToValidationProblem(v),
            NotFoundException => Problem(StatusCodes.Status404NotFound, "Resource not found", ex.Message),
            DomainException => Problem(StatusCodes.Status400BadRequest, "Business rule violation", ex.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Conflict", ex.Message),
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey } =>
                Problem(StatusCodes.Status409Conflict, "Duplicate",
                    "A document with the same unique key already exists"),
            MongoWriteException => Problem(StatusCodes.Status400BadRequest, "Write failed",
                "The database rejected the write operation"),
            MongoConnectionException or TimeoutException => Problem(StatusCodes.Status503ServiceUnavailable,
                "Database unavailable", "The database is temporarily unavailable. Try again later"),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected error",
                environment.IsDevelopment()
                    ? ex.Message
                    : "An unexpected error occurred. Use the correlation id when contacting support.")
        };

        if (body.Status >= 500)
            logger.LogError(ex, "Request {Path} failed with {Status}", http.Request.Path, body.Status);
        else
            logger.LogDebug("Request {Path} failed with {Status}: {Message}", http.Request.Path, body.Status, ex.Message);

        http.Response.StatusCode = body.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = body
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail)
        => new() { Status = status, Title = title, Detail = detail };

    private static ValidationProblemDetails ToValidationProblem(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "request" : e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error",
            Detail = "One or more validation errors occurred"
        };
    }
}
