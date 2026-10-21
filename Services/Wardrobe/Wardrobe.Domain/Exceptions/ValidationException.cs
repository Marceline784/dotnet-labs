namespace Wardrobe.Domain.Exceptions;

/// <summary>Некоректні вхідні дані -> HTTP 400.</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
