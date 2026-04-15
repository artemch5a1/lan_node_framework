using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Contracts.ScriptResultContract;

namespace Backend.Application.UseCases.ScriptResultUseCases.GetAllByStudent;

public record GetAllScriptResultByStudentQuery() : IQuery<List<ScriptResultDto>>;
