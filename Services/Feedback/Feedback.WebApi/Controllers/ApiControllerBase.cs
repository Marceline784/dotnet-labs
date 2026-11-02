using Feedback.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Feedback.WebApi.Controllers;

/// <summary>Базовий контролер: доступ до ISender (MediatR). Контролери лише передають команди й запити.</summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Читає очікувану версію документа із заголовка If-Match (наприклад, "3"). Немає заголовка або "*" означає
    /// «будь-яка версія». Некоректне значення дає 400.
    /// </summary>
    protected int? ReadIfMatchVersion()
    {
        if (!Request.Headers.TryGetValue("If-Match", out var values) || string.IsNullOrWhiteSpace(values.ToString()))
            return null;

        var raw = values.ToString().Trim().Trim('"');
        if (raw == "*") return null;

        return int.TryParse(raw, out var version) && version >= 0
            ? version
            : throw new DomainException("If-Match must contain the document version, for example \"3\"");
    }

    protected void SetETag(int version) => Response.Headers.ETag = $"\"{version}\"";
}
