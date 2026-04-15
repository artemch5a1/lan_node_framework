using Backend.Domain.Exceptions;
using Backend.Domain.Models.Base;

namespace Backend.Domain.Models;

public class ScriptResult : Entity
{
    public const string NameForMessage = "Результат задачи";

    private const int MaxSessionNameLength = 500;

    public Guid ExecutorId { get; private set; }

    public Executor Executor { get; private set; } = null!;

    public Guid ScriptId { get; private set; }

    public Script Script { get; private set; } = null!;

    public DateTimeOffset StartedAt { get; private set; }

    public string SessionName { get; private set; }

    public int PointScored { get; private set; }

    public int TotalPoint { get; private set; }

    private ScriptResult(
        Guid executorId,
        Guid scriptId,
        DateTimeOffset startedAt,
        string sessionName,
        int pointScored,
        int totalPoint
    )
    {
        ExecutorId = executorId;
        StartedAt = startedAt;
        SessionName = sessionName;
        PointScored = pointScored;
        TotalPoint = totalPoint;
        ScriptId = scriptId;
    }

    public static ScriptResult Create(
        Guid executorId,
        Guid scriptId,
        DateTimeOffset startedAt,
        string sessionName,
        int pointScored,
        int totalPoint
    )
    {
        if (sessionName.Count() > MaxSessionNameLength)
            throw new DomainException(
                $"Название сессии не может быть длиннее {MaxSessionNameLength} символов"
            );

        if (pointScored < 0 || totalPoint < 0)
            throw new DomainException($"Количество баллов не может быть меньше 0");

        if (pointScored > totalPoint)
            throw new DomainException(
                $"Количество набранных баллов не может быть больше общего количества баллов"
            );

        return new ScriptResult(
            executorId,
            scriptId,
            startedAt,
            sessionName,
            pointScored,
            totalPoint
        );
    }
}
