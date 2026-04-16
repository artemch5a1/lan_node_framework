namespace Backend.Application.Contracts;

public sealed record BookDto(int Id, string Title, string Author, int YearPublished);
