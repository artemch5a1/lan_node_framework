using Backend.Domain.Models.Base;

namespace Backend.Application.Abstractions.Repositories.Base;

/// <summary>
/// Контракт чтения сущности по идентификатору.
/// </summary>
public interface IReadable<T>
    where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
}
