using Backend.Application.Contracts.ExecutorContract;

namespace Backend.Application.Contracts.ScriptResultContract;

public class ScriptResultCreateDto
{
    public ExecutorCreateDto Executor { get; set; } = null!;

    public Guid ScriptId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public string SessionName { get; set; } = null!;

    public int PointScored { get; set; }

    public int TotalPoint { get; set; }
}
