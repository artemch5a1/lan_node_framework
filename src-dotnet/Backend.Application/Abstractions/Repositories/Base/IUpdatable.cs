using Backend.Domain.Models.Base;

namespace Backend.Application.Abstractions.Repositories.Base;

/// <summary>
/// Контракт обновления сущности.
/// </summary>
public interface IUpdatable<in T>
    where T : Entity
{
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
}
