using Backend.Application.Contracts.ExecutorContract;
using Backend.Application.Contracts.ScriptContract;

namespace Backend.Application.Contracts.ScriptResultContract;

public class ScriptResultDto
{
    public Guid Id { get; private set; }

    public Guid ExecutorId { get; set; }

    public ExecutorDto Executor { get; set; } = null!;

    public Guid ScriptId { get; set; }

    public ScriptDto Script { get; set; } = null!;

    public DateTimeOffset StartedAt { get; set; }

    public string SessionName { get; set; } = null!;

    public int PointScored { get; set; }

    public int TotalPoint { get; set; }
}
