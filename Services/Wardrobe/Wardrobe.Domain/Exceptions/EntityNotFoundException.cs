namespace Wardrobe.Domain.Exceptions;

/// <summary>Сутність не знайдено -> HTTP 404.</summary>
public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message) : base(message) { }

    public EntityNotFoundException(string entity, object key)
        : base($"{entity} with id '{key}' was not found") { }
}
