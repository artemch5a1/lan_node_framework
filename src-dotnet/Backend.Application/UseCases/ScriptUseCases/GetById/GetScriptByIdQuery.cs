using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Contracts.ScriptContract;

namespace Backend.Application.UseCases.ScriptUseCases.GetById;

public record GetScriptByIdQuery(Guid ScriptId) : IQuery<ScriptDto>;
