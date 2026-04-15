using Backend.Application.Contracts;
using Backend.Application.UseCases.Books.GetAll;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

/// <summary>Тестовый API: список книг (данные заданы в коде).</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class BooksController : ControllerBase
{
    private readonly ISender _sender;

    public BooksController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Возвращает коллекцию тестовых книг.</summary>
    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<BookDto>>> GetAll(
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<BookDto> books = await _sender.Send(
            new GetAllBooksQuery(),
            cancellationToken
        );
        return Ok(books);
    }
}
