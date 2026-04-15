using Backend.Domain.Exceptions;
using Backend.Domain.Models.Base;

namespace Backend.Domain.Models;

public class Script : Entity
{
    public const string NameForMessage = "Задача";

    private const int MaxTitleLength = 500;

    public string Title { get; private set; }

    private Script(string title)
    {
        Title = title;
    }

    public static Script Create(string title)
    {
        if (title.Count() > MaxTitleLength)
            throw new DomainException($"Название не может быть длиннее {MaxTitleLength} символов");

        return new Script(title);
    }
}
