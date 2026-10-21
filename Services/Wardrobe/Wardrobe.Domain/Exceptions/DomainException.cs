namespace Wardrobe.Domain.Exceptions;

/// <summary>Порушення бізнес-правила сутності (наприклад, річ уже в пранні) -> HTTP 409.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
