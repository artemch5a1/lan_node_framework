using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Contracts.ScriptContract;

namespace Backend.Application.UseCases.ScriptUseCases.GetAll;

public record GetAllScriptQuery() : IQuery<List<ScriptDto>>;
