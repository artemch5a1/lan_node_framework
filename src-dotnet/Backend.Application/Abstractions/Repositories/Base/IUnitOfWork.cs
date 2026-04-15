namespace Backend.Application.Abstractions.Repositories.Base;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    Task Rollback(CancellationToken cancellationToken = default);
}
