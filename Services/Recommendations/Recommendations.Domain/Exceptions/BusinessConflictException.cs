namespace Recommendations.Domain.Exceptions;

public sealed class BusinessConflictException : DomainException
{
    public BusinessConflictException(string message) : base(message) { }
}
