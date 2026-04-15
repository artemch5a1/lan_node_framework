namespace Backend.Application.Abstractions.Repositories.Base;

/// <summary>
/// Контракт удаления сущности по идентификатору.
/// </summary>
public interface IDeletable
{
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
