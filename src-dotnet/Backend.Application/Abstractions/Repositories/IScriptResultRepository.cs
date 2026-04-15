using Backend.Application.Abstractions.Repositories.Base;
using Backend.Domain.Models;

namespace Backend.Application.Abstractions.Repositories;

public interface IScriptResultRepository : ICreatable<ScriptResult>, IReadable<ScriptResult>
{
    Task<List<ScriptResult>> GetAllByStudentAsync(CancellationToken cancellationToken = default);
}
