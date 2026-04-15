namespace Backend.Application.Contracts.ScriptResultContract;

public class ScriptResultCreatedDto
{
    public Guid Id { get; private set; }

    public Guid ExecutorId { get; set; }

    public Guid ScriptId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public string SessionName { get; set; } = null!;

    public int PointScored { get; set; }

    public int TotalPoint { get; set; }
}
