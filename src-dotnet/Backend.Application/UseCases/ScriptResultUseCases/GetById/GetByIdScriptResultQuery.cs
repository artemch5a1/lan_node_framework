using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Contracts.ScriptResultContract;

namespace Backend.Application.UseCases.ScriptResultUseCases.GetById;

public record GetByIdScriptResultQuery(Guid ScriptResultId) : IQuery<ScriptResultDto>;
