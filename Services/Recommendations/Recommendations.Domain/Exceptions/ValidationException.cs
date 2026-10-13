namespace Recommendations.Domain.Exceptions;

public sealed class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }
}
