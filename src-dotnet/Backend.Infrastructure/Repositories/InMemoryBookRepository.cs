using Backend.Application.Abstractions;
using Backend.Domain.Models;

namespace Backend.Infrastructure.Repositories;

public sealed class InMemoryBookRepository : IBookRepository
{
    private static readonly IReadOnlyList<Book> Books =
    [
        new(1, "The Mythical Man-Month", "Frederick P. Brooks Jr.", 1975),
        new(2, "Clean Code", "Robert C. Martin", 2008),
        new(3, "Design Patterns", "Gang of Four", 1994),
        new(4, "Design Patterns", "Gang of Four", 1994),
        new(5, "Design Pфвфвatterns", "Gang of Four", 1994),
    ];

    public Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Books);
    }
}
