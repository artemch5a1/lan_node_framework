using Backend.Domain.Models;

namespace Backend.Application.Abstractions;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken);
}
