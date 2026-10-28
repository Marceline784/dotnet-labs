namespace Feedback.Domain.Exceptions;

/// <summary>Порушення бізнес-правила предметної області -> HTTP 400.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
