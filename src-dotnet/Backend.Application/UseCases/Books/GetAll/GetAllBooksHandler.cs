using Backend.Application.Abstractions;
using Backend.Application.Contracts;
using MediatR;

namespace Backend.Application.UseCases.Books.GetAll;

public sealed class GetAllBooksHandler(IBookRepository bookRepository)
    : IRequestHandler<GetAllBooksQuery, IReadOnlyList<BookDto>>
{
    public async Task<IReadOnlyList<BookDto>> Handle(
        GetAllBooksQuery request,
        CancellationToken cancellationToken
    )
    {
        var books = await bookRepository.GetAllAsync(cancellationToken);
        return books
            .Select(book => new BookDto(book.Id, book.Title, book.Author, book.YearPublished))
            .ToList();
    }
}
