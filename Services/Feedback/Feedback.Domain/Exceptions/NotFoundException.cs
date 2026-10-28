namespace Feedback.Domain.Exceptions;

/// <summary>Сутність не знайдено -> HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string entity, object key)
        : base($"{entity} with id '{key}' was not found") { }
}
