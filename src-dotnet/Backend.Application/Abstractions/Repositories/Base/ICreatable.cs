using Backend.Domain.Models.Base;

namespace Backend.Application.Abstractions.Repositories.Base;

/// <summary>
/// Контракт создания сущности.
/// </summary>
public interface ICreatable<T>
    where T : Entity
{
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
}
