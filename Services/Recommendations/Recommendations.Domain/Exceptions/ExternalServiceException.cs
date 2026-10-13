namespace Recommendations.Domain.Exceptions;

/// <summary>Зовнішній сервіс (наприклад, погода) недоступний -> HTTP 503.</summary>
public sealed class ExternalServiceException : DomainException
{
    public ExternalServiceException(string message) : base(message) { }
}
