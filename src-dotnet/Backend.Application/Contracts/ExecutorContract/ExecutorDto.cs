namespace Backend.Application.Contracts.ExecutorContract;

public class ExecutorDto
{
    public Guid Id { get; private set; }

    public string Surname { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Patronymic { get; set; } = null!;

    public string FullName => $"{Surname} {Name} {Patronymic}".Trim();
}
