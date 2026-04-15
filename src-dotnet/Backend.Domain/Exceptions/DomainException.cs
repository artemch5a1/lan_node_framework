namespace Backend.Domain.Exceptions;

/// <summary>
/// Ошибка нарушения бизнес правил
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message) { }
}
