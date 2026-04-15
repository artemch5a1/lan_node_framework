using Backend.Application.Contracts;
using MediatR;

namespace Backend.Application.UseCases.Books.GetAll;

public sealed record GetAllBooksQuery : IRequest<IReadOnlyList<BookDto>>;
