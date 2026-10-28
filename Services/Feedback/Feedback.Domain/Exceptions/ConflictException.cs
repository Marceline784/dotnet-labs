namespace Feedback.Domain.Exceptions;

/// <summary>Конфлікт (зокрема версій документа при оптимістичній конкурентності) -> HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
