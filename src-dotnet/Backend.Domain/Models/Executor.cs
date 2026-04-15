using Backend.Domain.Exceptions;
using Backend.Domain.Models.Base;

namespace Backend.Domain.Models;

public class Executor : Entity
{
    private const int MaxSurnameLength = 150;
    private const int MaxNameLength = 150;
    private const int MaxPatronymicLength = 150;

    public string Surname { get; private set; }

    public string Name { get; private set; }

    public string Patronymic { get; private set; }

    public string FullName => $"{Surname} {Name} {Patronymic}".Trim();

    private Executor(string surname, string name, string patronymic)
    {
        Surname = surname;
        Name = name;
        Patronymic = patronymic;
    }

    public static Executor Create(string surname, string name, string patronymic)
    {
        if (surname.Count() > MaxSurnameLength)
            throw new DomainException($"Фамилия не может быть длиннее {MaxSurnameLength} символов");

        if (name.Count() > MaxNameLength)
            throw new DomainException($"Имя не может быть длиннее {MaxNameLength} символов");

        if (patronymic.Count() > MaxPatronymicLength)
            throw new DomainException(
                $"Отчество не может быть длиннее {MaxPatronymicLength} символов"
            );

        return new Executor(surname, name, patronymic);
    }
}
