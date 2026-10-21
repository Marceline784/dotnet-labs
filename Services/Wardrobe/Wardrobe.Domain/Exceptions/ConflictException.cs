namespace Wardrobe.Domain.Exceptions;

/// <summary>Конфлікт зі станом даних (наприклад, дубль унікального значення) -> HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
