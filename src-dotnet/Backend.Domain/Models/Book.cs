namespace Backend.Domain.Models;

public sealed record Book(int Id, string Title, string Author, int YearPublished);
