namespace Backend.Application.Contracts.ScriptContract;

public class ScriptDto
{
    public Guid Id { get; private set; }
    public string Title { get; set; } = null!;
}
